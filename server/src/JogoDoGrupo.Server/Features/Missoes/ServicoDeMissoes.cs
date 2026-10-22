using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Missoes;

/// <summary>
/// Serviço autoritativo do motor de missões (#179, #1061).
/// </summary>
public sealed class ServicoDeMissoes
{
    private static readonly JsonSerializerOptions _jsonOpcoes = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _travasEntrega = new();

    private Func<DateTimeOffset> _relogioUtc = () => DateTimeOffset.UtcNow;

    public void DefinirRelogioDeTeste(Func<DateTimeOffset>? relogio)
    {
        _relogioUtc = relogio ?? (() => DateTimeOffset.UtcNow);
    }

    private readonly JogoDbContext _db;
    private readonly ICarteiraCoroas _carteira;
    private readonly IConcessaoDeItens _inventario;
    private readonly IPontosDeNivel _pontosDeNivel;
    private readonly IAjusteDeExperienciaDeTeste _experiencia;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly IConsultaDeGrupo? _grupos;
    private readonly IDiretorioDePersonagens? _diretorio;
    private readonly ILogger<ServicoDeMissoes> _logger;

    public ServicoDeMissoes(
        JogoDbContext db,
        ICarteiraCoroas carteira,
        IConcessaoDeItens inventario,
        IPontosDeNivel pontosDeNivel,
        IAjusteDeExperienciaDeTeste experiencia,
        IPosicoesDoMundo posicoes,
        ILogger<ServicoDeMissoes> logger,
        IConsultaDeGrupo? grupos = null,
        IDiretorioDePersonagens? diretorio = null
    )
    {
        _db = db;
        _carteira = carteira;
        _inventario = inventario;
        _pontosDeNivel = pontosDeNivel;
        _experiencia = experiencia;
        _posicoes = posicoes;
        _logger = logger;
        _grupos = grupos;
        _diretorio = diretorio;
    }

    public async Task<EstadoDasMissoes> GarantirMissaoInicialAsync(
        long personagemId,
        DateTimeOffset? momentoUtc = null,
        CancellationToken c = default
    )
    {
        var temMissoes = await _db.Set<MissaoPersonagemEntidade>()
            .AnyAsync(m => m.PersonagemId == personagemId, c);

        if (!temMissoes)
        {
            var def1 = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao1Chegada);
            if (def1 != null)
            {
                var objs = CriarProgressoInicial(def1);
                var ent = new MissaoPersonagemEntidade
                {
                    PersonagemId = personagemId,
                    MissaoId = def1.Id,
                    Estado = EstadoDaMissao.EmAndamento,
                    Acompanhada = true,
                    ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes),
                    AceitaEm = (momentoUtc ?? _relogioUtc()).ToUnixTimeSeconds(),
                };
                _db.Set<MissaoPersonagemEntidade>().Add(ent);
                await _db.SaveChangesAsync(c);
            }
        }

        return await ObterEstadoDasMissoesAsync(personagemId, momentoUtc, c);
    }

    public async Task<EstadoDasMissoes> ObterEstadoDasMissoesAsync(
        long personagemId,
        DateTimeOffset? momentoUtc = null,
        CancellationToken c = default
    )
    {
        var momento = momentoUtc ?? _relogioUtc();
        var agoraSecs = momento.ToUnixTimeSeconds();

        var todas = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId)
            .ToListAsync(c);

        var ativas = todas
            .Where(m => m.Estado != EstadoDaMissao.Concluida)
            .Select(MapearParaDto)
            .ToArray();

        var concluidas = todas
            .Where(m => m.Estado == EstadoDaMissao.Concluida)
            .Where(m =>
            {
                var def = CatalogoDeMissoes.Obter(m.MissaoId);
                if (def == null)
                    return false;
                if (def.Tipo == TipoDeMissao.Diaria)
                    return m.ConcluidaEm.HasValue
                        && RegrasCicloReset.MesmoCicloDiario(m.ConcluidaEm.Value, agoraSecs);
                if (def.Tipo == TipoDeMissao.Semanal)
                    return m.ConcluidaEm.HasValue
                        && RegrasCicloReset.MesmoCicloSemanal(m.ConcluidaEm.Value, agoraSecs);
                return true;
            })
            .Select(m => m.MissaoId)
            .ToArray();

        var concluidasPermanentes = todas
            .Where(m => m.Estado == EstadoDaMissao.Concluida)
            .Where(m =>
            {
                var def = CatalogoDeMissoes.Obter(m.MissaoId);
                return def != null
                    && (def.Tipo == TipoDeMissao.Principal || def.Tipo == TipoDeMissao.Secundaria);
            })
            .Select(m => m.MissaoId)
            .ToHashSet();

        var nivel = await _pontosDeNivel.ObterNivelAsync(personagemId, c);

        var rotDiaria = RegrasCicloReset.ObterIdsRotacaoDiaria(
            CatalogoDeMissoes.Todas,
            nivel,
            concluidasPermanentes,
            momento
        );

        var rotSemanal = RegrasCicloReset.ObterIdsRotacaoSemanal(
            CatalogoDeMissoes.Todas,
            nivel,
            concluidasPermanentes,
            momento
        );

        var proxDiario = RegrasCicloReset.ObterProximoResetDiario(momento).ToUnixTimeSeconds();
        var proxSemanal = RegrasCicloReset.ObterProximoResetSemanal(momento).ToUnixTimeSeconds();

        return new EstadoDasMissoes
        {
            Ativas = ativas,
            Concluidas = concluidas,
            RotacaoDiariaIds = rotDiaria.ToArray(),
            RotacaoSemanalIds = rotSemanal.ToArray(),
            ProximoResetDiarioSegundos = proxDiario,
            ProximoResetSemanalSegundos = proxSemanal,
        };
    }

    public async Task<(
        bool Sucesso,
        string? Erro,
        ProgressoDeMissaoDto? Missao
    )> AceitarMissaoAsync(
        long personagemId,
        int missaoId,
        DateTimeOffset? momentoUtc = null,
        CancellationToken c = default
    )
    {
        var momento = momentoUtc ?? _relogioUtc();
        var agoraSecs = momento.ToUnixTimeSeconds();

        var def = CatalogoDeMissoes.Obter(missaoId);
        if (def == null)
            return (false, CodigosErroMissoes.MissaoNaoEncontrada, null);

        if (_posicoes.TentarObter(personagemId, out var pos))
        {
            // Diária e semanal também saem do Quadro de Diárias (RegrasMissoes.OfereceNoNpc).
            var pertoDeQuemOferece =
                CatalogoNpcs.DentroDoAlcance(def.NpcDoadorId, (float)pos.X, (float)pos.Z)
                || (
                    RegrasMissoes.OfereceNoNpc(def, CatalogoNpcs.IdQuadroDeDiarias)
                    && CatalogoNpcs.DentroDoAlcance(
                        CatalogoNpcs.IdQuadroDeDiarias,
                        (float)pos.X,
                        (float)pos.Z
                    )
                );
            if (!pertoDeQuemOferece)
            {
                return (false, CodigosErroMissoes.LongeDoNpc, null);
            }
        }

        var nivel = await _pontosDeNivel.ObterNivelAsync(personagemId, c);
        var existentes = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId)
            .ToListAsync(c);

        var concluidas = existentes
            .Where(m => m.Estado == EstadoDaMissao.Concluida)
            .Select(m => m.MissaoId)
            .ToHashSet();

        var ativas = existentes
            .Where(m => m.Estado != EstadoDaMissao.Concluida)
            .Select(m => m.MissaoId)
            .ToHashSet();

        var concluidasNoCiclo = existentes
            .Where(m =>
            {
                if (m.Estado != EstadoDaMissao.Concluida || m.ConcluidaEm is not { } concluidaEm)
                    return false;
                var defM = CatalogoDeMissoes.Obter(m.MissaoId);
                if (defM == null)
                    return false;
                if (defM.Tipo == TipoDeMissao.Diaria)
                    return RegrasCicloReset.MesmoCicloDiario(concluidaEm, agoraSecs);
                if (defM.Tipo == TipoDeMissao.Semanal)
                    return RegrasCicloReset.MesmoCicloSemanal(concluidaEm, agoraSecs);
                return true;
            })
            .Select(m => m.MissaoId)
            .ToHashSet();

        var concluidasPermanentes = existentes
            .Where(m => m.Estado == EstadoDaMissao.Concluida)
            .Where(m =>
            {
                var defM = CatalogoDeMissoes.Obter(m.MissaoId);
                return defM != null
                    && (
                        defM.Tipo == TipoDeMissao.Principal || defM.Tipo == TipoDeMissao.Secundaria
                    );
            })
            .Select(m => m.MissaoId)
            .ToHashSet();

        HashSet<int>? rotacao = null;
        if (def.Tipo == TipoDeMissao.Diaria)
        {
            rotacao = RegrasCicloReset.ObterIdsRotacaoDiaria(
                CatalogoDeMissoes.Todas,
                nivel,
                concluidasPermanentes,
                momento
            );
        }
        else if (def.Tipo == TipoDeMissao.Semanal)
        {
            rotacao = RegrasCicloReset.ObterIdsRotacaoSemanal(
                CatalogoDeMissoes.Todas,
                nivel,
                concluidasPermanentes,
                momento
            );
        }

        if (
            !RegrasMissoes.PodeAceitar(
                def,
                nivel,
                concluidas,
                ativas,
                concluidasNoCiclo,
                rotacao,
                out var erro
            )
        )
        {
            return (false, erro, null);
        }

        var objs = CriarProgressoInicial(def);
        var acompanhadasCount = existentes.Count(m =>
            m.Acompanhada && m.Estado != EstadoDaMissao.Concluida
        );
        var acompanhada = acompanhadasCount < ConfigMissoes.MaximoMissoesAcompanhadas;

        var ent = existentes.FirstOrDefault(m => m.MissaoId == missaoId);
        if (ent == null)
        {
            ent = new MissaoPersonagemEntidade
            {
                PersonagemId = personagemId,
                MissaoId = missaoId,
                Estado = EstadoDaMissao.EmAndamento,
                Acompanhada = acompanhada,
                ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes),
                AceitaEm = agoraSecs,
            };
            _db.Set<MissaoPersonagemEntidade>().Add(ent);
        }
        else
        {
            ent.Estado = EstadoDaMissao.EmAndamento;
            ent.Acompanhada = acompanhada;
            ent.ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes);
            ent.AceitaEm = agoraSecs;
            ent.ConcluidaEm = null;
        }

        await _db.SaveChangesAsync(c);
        return (true, null, MapearParaDto(ent));
    }

    public async Task<(bool Sucesso, string? Erro)> AbandonarMissaoAsync(
        long personagemId,
        int missaoId,
        CancellationToken c = default
    )
    {
        var ent = await _db.Set<MissaoPersonagemEntidade>()
            .FirstOrDefaultAsync(
                m =>
                    m.PersonagemId == personagemId
                    && m.MissaoId == missaoId
                    && m.Estado != EstadoDaMissao.Concluida,
                c
            );

        if (ent == null)
            return (false, CodigosErroMissoes.MissaoNaoEncontrada);

        var def = CatalogoDeMissoes.Obter(missaoId);
        if (def == null)
            return (false, CodigosErroMissoes.MissaoNaoEncontrada);

        if (!RegrasMissoes.PodeAbandonar(def, out var erro))
        {
            return (false, erro);
        }

        _db.Set<MissaoPersonagemEntidade>().Remove(ent);
        await _db.SaveChangesAsync(c);
        return (true, null);
    }

    public async Task<(
        bool Sucesso,
        string? Erro,
        ProgressoDeMissaoDto? Concluida,
        ProgressoDeMissaoDto? Proxima
    )> EntregarMissaoAsync(
        long personagemId,
        int missaoId,
        int? itemIdEscolhido,
        DateTimeOffset? momentoUtc = null,
        CancellationToken c = default
    )
    {
        var momento = momentoUtc ?? _relogioUtc();
        var agoraSecs = momento.ToUnixTimeSeconds();

        var trava = _travasEntrega.GetOrAdd(
            $"entrega:{personagemId}:{missaoId}",
            _ => new SemaphoreSlim(1, 1)
        );
        await trava.WaitAsync(c);
        try
        {
            var ent = await _db.Set<MissaoPersonagemEntidade>()
                .FirstOrDefaultAsync(
                    m =>
                        m.PersonagemId == personagemId
                        && m.MissaoId == missaoId
                        && m.Estado != EstadoDaMissao.Concluida,
                    c
                );

            if (ent == null)
                return (false, CodigosErroMissoes.MissaoNaoEncontrada, null, null);

            var def = CatalogoDeMissoes.Obter(missaoId);
            if (def == null)
                return (false, CodigosErroMissoes.MissaoNaoEncontrada, null, null);

            if (_posicoes.TentarObter(personagemId, out var pos))
            {
                if (!CatalogoNpcs.DentroDoAlcance(def.NpcEntregadorId, (float)pos.X, (float)pos.Z))
                {
                    return (false, CodigosErroMissoes.LongeDoNpc, null, null);
                }
            }

            var dto = MapearParaDto(ent);
            if (!RegrasMissoes.PodeEntregar(def, dto, itemIdEscolhido, out var erro))
            {
                return (false, erro, null, null);
            }

            // Transação que envolve _db (entidade de missão, carteira e inventário usam o mesmo DbContext)
            await using var tx = await _db.Database.BeginTransactionAsync(c);

            ent.Estado = EstadoDaMissao.Concluida;
            ent.ConcluidaEm = agoraSecs;
            ent.Acompanhada = false;

            if (def.RecompensaCoroas > 0)
            {
                await _carteira.CreditarAsync(personagemId, def.RecompensaCoroas, c);
            }

            var destinoDoItem = DestinoDaConcessao.Recusado;
            if (itemIdEscolhido.HasValue && itemIdEscolhido.Value > 0)
            {
                // Arma de quem está sem arma entra equipada no conjunto 1; o resto, na bolsa.
                destinoDoItem = await _inventario.ConcederAsync(
                    personagemId,
                    itemIdEscolhido.Value,
                    c
                );
            }

            // Cadeia principal: se há próxima missão principal, cria junto
            ProgressoDeMissaoDto? proxima = null;
            if (def.ProximaMissaoPrincipalId > 0)
            {
                var defProx = CatalogoDeMissoes.Obter(def.ProximaMissaoPrincipalId);
                if (defProx != null)
                {
                    var proxObjs = CriarProgressoInicial(defProx);
                    var proxEnt = new MissaoPersonagemEntidade
                    {
                        PersonagemId = personagemId,
                        MissaoId = defProx.Id,
                        Estado = EstadoDaMissao.EmAndamento,
                        Acompanhada = true,
                        ObjetivosJson = JsonSerializer.Serialize(proxObjs, _jsonOpcoes),
                        AceitaEm = agoraSecs,
                    };
                    _db.Set<MissaoPersonagemEntidade>().Add(proxEnt);
                    proxima = MapearParaDto(proxEnt);
                }
            }

            await _db.SaveChangesAsync(c);
            await tx.CommitAsync(c);

            // Só depois de gravado: visual da área e barra de skills da arma nova.
            if (destinoDoItem == DestinoDaConcessao.Equipado)
            {
                await _inventario.AvisarAsync(personagemId, c);
            }

            // Concessão de XP fora da transação de _db (possui escopo próprio de banco em Personagem)
            if (def.RecompensaXp > 0)
            {
                await _experiencia.AjustarAsync(personagemId, def.RecompensaXp, c);
            }

            if (!string.IsNullOrEmpty(def.ReputacaoFaccao) && def.ReputacaoValor > 0)
            {
                _logger.LogInformation(
                    "Personagem {PersonagemId} recebeu +{Valor} reputação com {Faccao} na entrega da missão {MissaoId}",
                    personagemId,
                    def.ReputacaoValor,
                    def.ReputacaoFaccao,
                    missaoId
                );
            }

            return (true, null, MapearParaDto(ent), proxima);
        }
        finally
        {
            trava.Release();
        }
    }

    public async Task<(bool Sucesso, string? Erro)> AlternarAcompanhamentoAsync(
        long personagemId,
        int missaoId,
        bool acompanhada,
        CancellationToken c = default
    )
    {
        var ent = await _db.Set<MissaoPersonagemEntidade>()
            .FirstOrDefaultAsync(
                m =>
                    m.PersonagemId == personagemId
                    && m.MissaoId == missaoId
                    && m.Estado != EstadoDaMissao.Concluida,
                c
            );

        if (ent == null)
            return (false, CodigosErroMissoes.MissaoNaoEncontrada);

        if (acompanhada)
        {
            var count = await _db.Set<MissaoPersonagemEntidade>()
                .CountAsync(
                    m =>
                        m.PersonagemId == personagemId
                        && m.Acompanhada
                        && m.Estado != EstadoDaMissao.Concluida
                        && m.MissaoId != missaoId,
                    c
                );

            if (count >= ConfigMissoes.MaximoMissoesAcompanhadas)
            {
                return (false, CodigosErroMissoes.LimiteRastreadorAtingido);
            }
        }

        ent.Acompanhada = acompanhada;
        await _db.SaveChangesAsync(c);
        return (true, null);
    }

    public async Task<List<ProgressoDeMissaoDto>> RegistrarMorteInimigoAsync(
        long personagemId,
        int criaturaId,
        CancellationToken c = default
    )
    {
        var ativas = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId && m.Estado == EstadoDaMissao.EmAndamento)
            .ToListAsync(c);

        var atualizadas = new List<ProgressoDeMissaoDto>();

        foreach (var ent in ativas)
        {
            var def = CatalogoDeMissoes.Obter(ent.MissaoId);
            if (def == null)
                continue;

            var objs = DeserializarObjetivos(ent.ObjetivosJson);
            var mudou = false;

            for (int i = 0; i < def.Objetivos.Length; i++)
            {
                var objDef = def.Objetivos[i];
                if (
                    objDef.Tipo == TipoDeObjetivo.Matar
                    && (objDef.AlvoId == criaturaId || objDef.AlvoId == 0)
                )
                {
                    var objProg = objs.FirstOrDefault(o => o.ObjetivoId == objDef.Id);
                    if (objProg != null && !objProg.Concluido)
                    {
                        objProg.QuantidadeAtual++;
                        if (objProg.QuantidadeAtual >= objDef.QuantidadeTotal)
                        {
                            objProg.QuantidadeAtual = objDef.QuantidadeTotal;
                            objProg.Concluido = true;
                        }
                        mudou = true;
                    }
                }
            }

            if (mudou)
            {
                ent.ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes);
                if (objs.All(o => o.Concluido))
                {
                    ent.Estado = EstadoDaMissao.ProntaParaEntregar;
                }
                atualizadas.Add(MapearParaDto(ent));
            }
        }

        if (atualizadas.Count > 0)
        {
            await _db.SaveChangesAsync(c);
        }

        return atualizadas;
    }

    public async Task<List<ProgressoDeMissaoDto>> RegistrarInteracaoNpcAsync(
        long personagemId,
        int npcId,
        CancellationToken c = default
    )
    {
        var ativas = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId && m.Estado == EstadoDaMissao.EmAndamento)
            .ToListAsync(c);

        var atualizadas = new List<ProgressoDeMissaoDto>();

        foreach (var ent in ativas)
        {
            var def = CatalogoDeMissoes.Obter(ent.MissaoId);
            if (def == null)
                continue;

            var objs = DeserializarObjetivos(ent.ObjetivosJson);
            var mudou = false;

            for (int i = 0; i < def.Objetivos.Length; i++)
            {
                var objDef = def.Objetivos[i];
                if (objDef.Tipo == TipoDeObjetivo.Falar && objDef.AlvoId == npcId)
                {
                    var objProg = objs.FirstOrDefault(o => o.ObjetivoId == objDef.Id);
                    if (objProg != null && !objProg.Concluido)
                    {
                        objProg.QuantidadeAtual = 1;
                        objProg.Concluido = true;
                        mudou = true;
                    }
                }
            }

            if (mudou)
            {
                ent.ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes);
                if (objs.All(o => o.Concluido))
                {
                    ent.Estado = EstadoDaMissao.ProntaParaEntregar;
                }
                atualizadas.Add(MapearParaDto(ent));
            }
        }

        if (atualizadas.Count > 0)
        {
            await _db.SaveChangesAsync(c);
        }

        return atualizadas;
    }

    public async Task<List<ProgressoDeMissaoDto>> RegistrarColetaItemAsync(
        long personagemId,
        int itemId,
        int quantidade = 1,
        CancellationToken c = default
    )
    {
        if (quantidade <= 0)
            return new List<ProgressoDeMissaoDto>();

        var ativas = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId && m.Estado == EstadoDaMissao.EmAndamento)
            .ToListAsync(c);

        var atualizadas = new List<ProgressoDeMissaoDto>();

        foreach (var ent in ativas)
        {
            var def = CatalogoDeMissoes.Obter(ent.MissaoId);
            if (def == null)
                continue;

            var objs = DeserializarObjetivos(ent.ObjetivosJson);
            var mudou = false;

            for (int i = 0; i < def.Objetivos.Length; i++)
            {
                var objDef = def.Objetivos[i];
                if (
                    objDef.Tipo == TipoDeObjetivo.Coletar
                    && (objDef.AlvoId == itemId || objDef.AlvoId == 0)
                )
                {
                    var objProg = objs.FirstOrDefault(o => o.ObjetivoId == objDef.Id);
                    if (objProg != null && !objProg.Concluido)
                    {
                        objProg.QuantidadeAtual += quantidade;
                        if (objProg.QuantidadeAtual >= objDef.QuantidadeTotal)
                        {
                            objProg.QuantidadeAtual = objDef.QuantidadeTotal;
                            objProg.Concluido = true;
                        }
                        mudou = true;
                    }
                }
            }

            if (mudou)
            {
                ent.ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes);
                if (objs.All(o => o.Concluido))
                {
                    ent.Estado = EstadoDaMissao.ProntaParaEntregar;
                }
                atualizadas.Add(MapearParaDto(ent));
            }
        }

        if (atualizadas.Count > 0)
        {
            await _db.SaveChangesAsync(c);
        }

        return atualizadas;
    }

    public async Task<List<ProgressoDeMissaoDto>> RegistrarChegadaLocalAsync(
        long personagemId,
        float x,
        float z,
        string? regiao = null,
        CancellationToken c = default
    )
    {
        var ativas = await _db.Set<MissaoPersonagemEntidade>()
            .Where(m => m.PersonagemId == personagemId && m.Estado == EstadoDaMissao.EmAndamento)
            .ToListAsync(c);

        var atualizadas = new List<ProgressoDeMissaoDto>();

        foreach (var ent in ativas)
        {
            var def = CatalogoDeMissoes.Obter(ent.MissaoId);
            if (def == null)
                continue;

            var objs = DeserializarObjetivos(ent.ObjetivosJson);
            var mudou = false;

            for (int i = 0; i < def.Objetivos.Length; i++)
            {
                var objDef = def.Objetivos[i];
                if (objDef.Tipo == TipoDeObjetivo.IrA)
                {
                    if (
                        !string.IsNullOrEmpty(regiao)
                        && !string.IsNullOrEmpty(objDef.RegiaoAlvo)
                        && !string.Equals(
                            objDef.RegiaoAlvo,
                            regiao,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    var dx = x - objDef.CoordenadaX;
                    var dz = z - objDef.CoordenadaZ;
                    var distSq = dx * dx + dz * dz;
                    var raio = objDef.RaioAreaMetros > 0f ? objDef.RaioAreaMetros : 5f;

                    if (distSq <= raio * raio)
                    {
                        var objProg = objs.FirstOrDefault(o => o.ObjetivoId == objDef.Id);
                        if (objProg != null && !objProg.Concluido)
                        {
                            objProg.QuantidadeAtual = 1;
                            objProg.Concluido = true;
                            mudou = true;
                        }
                    }
                }
            }

            if (mudou)
            {
                ent.ObjetivosJson = JsonSerializer.Serialize(objs, _jsonOpcoes);
                if (objs.All(o => o.Concluido))
                {
                    ent.Estado = EstadoDaMissao.ProntaParaEntregar;
                }
                atualizadas.Add(MapearParaDto(ent));
            }
        }

        if (atualizadas.Count > 0)
        {
            await _db.SaveChangesAsync(c);
        }

        return atualizadas;
    }

    public async Task<(
        bool Sucesso,
        string? Erro,
        List<long>? Destinatarios,
        string? Titulo
    )> CompartilharComGrupoAsync(long personagemId, int missaoId, CancellationToken c = default)
    {
        var def = CatalogoDeMissoes.Obter(missaoId);
        if (def == null)
            return (false, CodigosErroMissoes.MissaoNaoEncontrada, null, null);

        if (_grupos == null)
            return (false, CodigosErroMissoes.ApenasLiderCompartilha, null, null);

        var grupo = _grupos.Consultar(personagemId);
        if (grupo == null || grupo.LiderId != personagemId)
        {
            return (false, CodigosErroMissoes.ApenasLiderCompartilha, null, null);
        }

        var destinatarios = new List<long>();
        PosicaoNoMundo? posLider = null;
        if (_posicoes.TentarObter(personagemId, out var pLider))
        {
            posLider = pLider;
        }

        foreach (var membroId in grupo.Membros)
        {
            if (membroId == personagemId)
                continue;

            if (posLider.HasValue && _posicoes.TentarObter(membroId, out var posMembro))
            {
                var dx = (float)(posLider.Value.X - posMembro.X);
                var dz = (float)(posLider.Value.Z - posMembro.Z);
                var dist = MathF.Sqrt(dx * dx + dz * dz);
                if (dist > ConfigMissoes.RaioCompartilhamentoMetros)
                    continue;
            }

            destinatarios.Add(membroId);
        }

        return (true, null, destinatarios, def.Titulo);
    }

    private static ProgressoDeObjetivo[] CriarProgressoInicial(DefinicaoDeMissao def)
    {
        var lista = new ProgressoDeObjetivo[def.Objetivos.Length];
        for (int i = 0; i < def.Objetivos.Length; i++)
        {
            lista[i] = new ProgressoDeObjetivo
            {
                ObjetivoId = def.Objetivos[i].Id,
                QuantidadeAtual = 0,
                Concluido = false,
            };
        }
        return lista;
    }

    private static ProgressoDeObjetivo[] DeserializarObjetivos(string json)
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<ProgressoDeObjetivo>();

        try
        {
            return JsonSerializer.Deserialize<ProgressoDeObjetivo[]>(json, _jsonOpcoes)
                ?? Array.Empty<ProgressoDeObjetivo>();
        }
        catch
        {
            return Array.Empty<ProgressoDeObjetivo>();
        }
    }

    private static ProgressoDeMissaoDto MapearParaDto(MissaoPersonagemEntidade entidade)
    {
        return new ProgressoDeMissaoDto
        {
            MissaoId = entidade.MissaoId,
            Estado = entidade.Estado,
            Acompanhada = entidade.Acompanhada,
            Objetivos = DeserializarObjetivos(entidade.ObjetivosJson),
        };
    }
}
