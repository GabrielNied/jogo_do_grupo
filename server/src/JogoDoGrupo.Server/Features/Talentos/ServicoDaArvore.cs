using System.Data;
using System.Text.Json;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>Resultado de uma compra: o estado novo, ou o código de erro.</summary>
public sealed record ResultadoDaCompra(ArvoreResponse? Arvore, string? Erro);

/// <summary>Resultado de uma redistribuição: o estado novo, ou o código de erro.</summary>
public sealed record ResultadoDaRedistribuicao(ArvoreResponse? Arvore, string? Erro);

/// <summary>Resultado ao salvar um perfil.</summary>
public sealed record ResultadoSalvarPerfil(PerfilTalentosDto? Perfil, string? Erro);

/// <summary>Resultado ao carregar um perfil.</summary>
public sealed record ResultadoCarregarPerfil(CarregarPerfilResponse? Resposta, string? Erro);

/// <summary>Resultado simples de operações com perfis (renomear, excluir).</summary>
public sealed record ResultadoPerfil(string? Erro);

/// <summary>
/// Lê, compra ranks, redistribui e gerencia perfis da árvore de talentos. Usa o mesmo contexto da
/// requisição, garantindo atomicidade com Personagem (<see cref="IPontosDeNivel"/>) e Economia (<see cref="ICarteiraCoroas"/>).
/// </summary>
public sealed class ServicoDaArvore(
    JogoDbContext db,
    IPontosDeNivel pontos,
    ConfiguracaoDaArvore configuracao,
    ICarteiraCoroas carteira,
    IEstadoDeCombate? combate = null
)
{
    public async Task<ArvoreResponse> ObterAsync(long personagemId, CancellationToken cancelamento)
    {
        await MigrarSeDesatualizadaAsync(personagemId, cancelamento);
        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .AsNoTracking()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);
        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        return Montar(cabecalho, nos, livres, await CustoAsync(personagemId, cancelamento));
    }

    /// <summary>
    /// Compra o rank pedido ou não muda nada. A transação Serializable abre com a trava de
    /// escrita no SQLite (<c>BEGIN IMMEDIATE</c>): dois pedidos do mesmo personagem rodam um
    /// depois do outro, e o segundo já vê o rank que o primeiro comprou (e só responde sucesso).
    /// </summary>
    public async Task<ResultadoDaCompra> ComprarAsync(
        long personagemId,
        ComprarNoRequest pedido,
        CancellationToken cancelamento
    )
    {
        await MigrarSeDesatualizadaAsync(personagemId, cancelamento);
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);

        if (pedido.VersaoConfiguracao != configuracao.Versao)
        {
            return new ResultadoDaCompra(null, CodigosErroTalentos.ArvoreDesatualizada);
        }

        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        var ranks = RegrasDaArvore.ParaDicionario(Ranks(nos));
        var avaliacao = RegrasDaArvore.AvaliarCompra(
            configuracao,
            ranks,
            livres,
            pedido.NoId,
            pedido.RankAlvo
        );
        if (avaliacao.Erro != null)
        {
            return new ResultadoDaCompra(null, avaliacao.Erro);
        }

        if (avaliacao.JaComprado)
        {
            return new ResultadoDaCompra(
                Montar(cabecalho, nos, livres, await CustoAsync(personagemId, cancelamento)),
                null
            );
        }

        if (!await pontos.TentarGastarAsync(personagemId, avaliacao.Custo, cancelamento))
        {
            return new ResultadoDaCompra(null, CodigosErroTalentos.PontosInsuficientes);
        }

        if (cabecalho == null)
        {
            cabecalho = new ArvoreDoPersonagem
            {
                PersonagemId = personagemId,
                Versao = configuracao.Versao,
            };
            db.Add(cabecalho);
        }

        cabecalho.PontosGastos += avaliacao.Custo;

        var no = nos.FirstOrDefault(n => n.NoId == pedido.NoId);
        if (no == null)
        {
            no = new NoComprado { PersonagemId = personagemId, NoId = pedido.NoId };
            nos.Add(no);
            db.Add(no);
        }

        no.Rank = pedido.RankAlvo;

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);
        return new ResultadoDaCompra(
            Montar(
                cabecalho,
                nos,
                livres - avaliacao.Custo,
                await CustoAsync(personagemId, cancelamento)
            ),
            null
        );
    }

    /// <summary>
    /// Redistribui todos os pontos gastos da árvore: cobra o custo em coroas via <see cref="ICarteiraCoroas"/>,
    /// remove os nós comprados e devolve os pontos para os pontos livres de Personagem.
    /// Só permitido fora de combate. Cobrança e redistribuição acontecem na mesma transação.
    /// </summary>
    public async Task<ResultadoDaRedistribuicao> RedistribuirAsync(
        long personagemId,
        RedistribuirRequest pedido,
        CancellationToken cancelamento
    )
    {
        if (combate != null && combate.EmCombate(personagemId))
        {
            return new ResultadoDaRedistribuicao(null, CodigosErroTalentos.EmCombate);
        }

        await MigrarSeDesatualizadaAsync(personagemId, cancelamento);
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);

        if (pedido.VersaoConfiguracao != configuracao.Versao)
        {
            return new ResultadoDaRedistribuicao(null, CodigosErroTalentos.ArvoreDesatualizada);
        }

        var pontosGastos = cabecalho?.PontosGastos ?? 0;
        if (pontosGastos <= 0 && nos.Count == 0)
        {
            return new ResultadoDaRedistribuicao(null, CodigosErroTalentos.NenhumPontoGasto);
        }

        var custo = await CustoAsync(personagemId, cancelamento);
        var cobrou = await carteira.DebitarAsync(personagemId, custo, cancelamento);
        if (!cobrou)
        {
            return new ResultadoDaRedistribuicao(null, CodigosErroTalentos.SaldoInsuficiente);
        }

        if (nos.Count > 0)
        {
            db.Set<NoComprado>().RemoveRange(nos);
            nos.Clear();
        }

        if (cabecalho != null)
        {
            cabecalho.PontosGastos = 0;
        }

        if (pontosGastos > 0)
        {
            await pontos.DevolverAsync(personagemId, pontosGastos, cancelamento);
        }

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        return new ResultadoDaRedistribuicao(Montar(cabecalho, nos, livres, custo), null);
    }

    /// <summary>
    /// Migração da configuração (#627): uma árvore comprada em outra versão não vale na atual,
    /// então os ranks saem e os pontos pagos voltam inteiros para os livres, sem cobrar coroas
    /// (a mudança foi do jogo, não do jogador). Roda ao ler, comprar, redistribuir e carregar
    /// perfil; sem árvore salva ou na versão atual não faz nada. Os perfis salvos seguem a
    /// validação própria ao carregar (nó sumido gera aviso, ponto não some).
    /// </summary>
    private async Task MigrarSeDesatualizadaAsync(long personagemId, CancellationToken cancelamento)
    {
        var versaoSalva = await db.Set<ArvoreDoPersonagem>()
            .AsNoTracking()
            .Where(a => a.PersonagemId == personagemId)
            .Select(a => (int?)a.Versao)
            .FirstOrDefaultAsync(cancelamento);
        if (versaoSalva == null || versaoSalva == configuracao.Versao)
        {
            return;
        }

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        if (cabecalho == null || cabecalho.Versao == configuracao.Versao)
        {
            return;
        }

        var nos = await db.Set<NoComprado>()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);
        db.Set<NoComprado>().RemoveRange(nos);
        var reembolso = cabecalho.PontosGastos;
        cabecalho.PontosGastos = 0;
        cabecalho.Versao = configuracao.Versao;
        if (reembolso > 0)
        {
            await pontos.DevolverAsync(personagemId, reembolso, cancelamento);
        }

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);
    }

    /// <summary>Lista todos os perfis de build salvos do personagem.</summary>
    public async Task<ListaPerfisResponse> ListarPerfisAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var perfis = await db.Set<PerfilDeTalentos>()
            .AsNoTracking()
            .Where(p => p.PersonagemId == personagemId)
            .OrderBy(p => p.CriadoEm)
            .ToListAsync(cancelamento);

        var lista = new List<PerfilTalentosDto>();
        foreach (var p in perfis)
        {
            var ranks = DeserializarRanks(p.RanksJson);
            var pontosTotais = RegrasDaArvore.CalcularPontosGastos(
                configuracao,
                RegrasDaArvore.ParaDicionario(ranks)
            );

            lista.Add(
                new PerfilTalentosDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    VersaoConfiguracao = p.Versao,
                    CriadoEm = p.CriadoEm.ToString("O"),
                    Ranks = ranks,
                    PontosTotais = pontosTotais,
                }
            );
        }

        return new ListaPerfisResponse { Perfis = lista.ToArray() };
    }

    /// <summary>Salva a distribuição atual de talentos do personagem como um novo perfil.</summary>
    public async Task<ResultadoSalvarPerfil> SalvarPerfilAsync(
        long personagemId,
        SalvarPerfilRequest pedido,
        CancellationToken cancelamento
    )
    {
        var nome = pedido.Nome?.Trim();
        if (
            string.IsNullOrEmpty(nome)
            || nome.Length > ConfigRedistribuicao.TamanhoMaximoNomePerfil
        )
        {
            return new ResultadoSalvarPerfil(null, CodigosErroTalentos.NomeDePerfilInvalido);
        }

        var total = await db.Set<PerfilDeTalentos>()
            .CountAsync(p => p.PersonagemId == personagemId, cancelamento);
        if (total >= ConfigRedistribuicao.LimitePerfis)
        {
            return new ResultadoSalvarPerfil(null, CodigosErroTalentos.LimiteDePerfisAtingido);
        }

        var nos = await db.Set<NoComprado>()
            .AsNoTracking()
            .Where(n => n.PersonagemId == personagemId && n.Rank > 0)
            .ToListAsync(cancelamento);

        var ranks = Ranks(nos);
        var novo = new PerfilDeTalentos
        {
            PersonagemId = personagemId,
            Nome = nome,
            Versao = configuracao.Versao,
            CriadoEm = DateTime.UtcNow,
            RanksJson = JsonSerializer.Serialize(ranks, OpcoesJson),
        };

        db.Add(novo);
        await db.SaveChangesAsync(cancelamento);

        var pontosTotais = RegrasDaArvore.CalcularPontosGastos(
            configuracao,
            RegrasDaArvore.ParaDicionario(ranks)
        );

        var dto = new PerfilTalentosDto
        {
            Id = novo.Id,
            Nome = novo.Nome,
            VersaoConfiguracao = novo.Versao,
            CriadoEm = novo.CriadoEm.ToString("O"),
            Ranks = ranks,
            PontosTotais = pontosTotais,
        };

        return new ResultadoSalvarPerfil(dto, null);
    }

    /// <summary>Renomeia um perfil de build existente.</summary>
    public async Task<ResultadoPerfil> RenomearPerfilAsync(
        long personagemId,
        long perfilId,
        RenomearPerfilRequest pedido,
        CancellationToken cancelamento
    )
    {
        var nome = pedido.Nome?.Trim();
        if (
            string.IsNullOrEmpty(nome)
            || nome.Length > ConfigRedistribuicao.TamanhoMaximoNomePerfil
        )
        {
            return new ResultadoPerfil(CodigosErroTalentos.NomeDePerfilInvalido);
        }

        var perfil = await db.Set<PerfilDeTalentos>()
            .FirstOrDefaultAsync(
                p => p.Id == perfilId && p.PersonagemId == personagemId,
                cancelamento
            );
        if (perfil == null)
        {
            return new ResultadoPerfil(CodigosErroTalentos.PerfilNaoEncontrado);
        }

        perfil.Nome = nome;
        await db.SaveChangesAsync(cancelamento);
        return new ResultadoPerfil(null);
    }

    /// <summary>Exclui um perfil de build.</summary>
    public async Task<ResultadoPerfil> ExcluirPerfilAsync(
        long personagemId,
        long perfilId,
        CancellationToken cancelamento
    )
    {
        var perfil = await db.Set<PerfilDeTalentos>()
            .FirstOrDefaultAsync(
                p => p.Id == perfilId && p.PersonagemId == personagemId,
                cancelamento
            );
        if (perfil == null)
        {
            return new ResultadoPerfil(CodigosErroTalentos.PerfilNaoEncontrado);
        }

        db.Remove(perfil);
        await db.SaveChangesAsync(cancelamento);
        return new ResultadoPerfil(null);
    }

    /// <summary>
    /// Carrega e aplica um perfil de build no personagem.
    /// Valida os ranks contra a versão atual e os pontos totais (livres + gastos).
    /// Nós que não existem mais ou cujos requisitos foram quebrados geram aviso e têm seus pontos preservados.
    /// </summary>
    public async Task<ResultadoCarregarPerfil> CarregarPerfilAsync(
        long personagemId,
        long perfilId,
        CancellationToken cancelamento
    )
    {
        if (combate != null && combate.EmCombate(personagemId))
        {
            return new ResultadoCarregarPerfil(null, CodigosErroTalentos.EmCombate);
        }

        var perfil = await db.Set<PerfilDeTalentos>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == perfilId && p.PersonagemId == personagemId,
                cancelamento
            );
        if (perfil == null)
        {
            return new ResultadoCarregarPerfil(null, CodigosErroTalentos.PerfilNaoEncontrado);
        }

        await MigrarSeDesatualizadaAsync(personagemId, cancelamento);
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);

        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        var pontosGastosAtuais = cabecalho?.PontosGastos ?? 0;
        var totalPontosDisponiveis = livres + pontosGastosAtuais;

        var ranksDoPerfil = DeserializarRanks(perfil.RanksJson);
        var validacao = RegrasDaArvore.ValidarECalcularPerfil(
            configuracao,
            ranksDoPerfil,
            totalPontosDisponiveis
        );
        if (validacao.Erro != null)
        {
            return new ResultadoCarregarPerfil(null, validacao.Erro);
        }

        // Carregar um perfil que muda a distribuição custa o mesmo que redistribuir: senão o perfil
        // seria um jeito grátis de trocar de build e a cobrança por nível não valeria nada (#910).
        var atuais = nos.Where(n => n.Rank > 0).ToDictionary(n => n.NoId, n => n.Rank);
        var muda =
            atuais.Count != validacao.RanksAplicaveis.Count
            || validacao.RanksAplicaveis.Any(par =>
                !atuais.TryGetValue(par.Key, out var rank) || rank != par.Value
            );
        long cobrado = 0;
        if (muda)
        {
            cobrado = await CustoAsync(personagemId, cancelamento);
            if (!await carteira.DebitarAsync(personagemId, cobrado, cancelamento))
            {
                return new ResultadoCarregarPerfil(null, CodigosErroTalentos.SaldoInsuficiente);
            }
        }

        // Remove os nós atuais
        if (nos.Count > 0)
        {
            db.Set<NoComprado>().RemoveRange(nos);
            nos.Clear();
        }

        // Insere os novos nós validados
        foreach (var par in validacao.RanksAplicaveis)
        {
            var novoNo = new NoComprado
            {
                PersonagemId = personagemId,
                NoId = par.Key,
                Rank = par.Value,
            };
            nos.Add(novoNo);
            db.Add(novoNo);
        }

        if (cabecalho == null)
        {
            cabecalho = new ArvoreDoPersonagem
            {
                PersonagemId = personagemId,
                Versao = configuracao.Versao,
            };
            db.Add(cabecalho);
        }
        else
        {
            cabecalho.Versao = configuracao.Versao;
        }

        var diferenca = validacao.CustoTotal - pontosGastosAtuais;
        cabecalho.PontosGastos = validacao.CustoTotal;

        if (diferenca > 0)
        {
            if (!await pontos.TentarGastarAsync(personagemId, diferenca, cancelamento))
            {
                return new ResultadoCarregarPerfil(null, CodigosErroTalentos.PontosInsuficientes);
            }
        }
        else if (diferenca < 0)
        {
            await pontos.DevolverAsync(personagemId, -diferenca, cancelamento);
        }

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        var respostaArvore = Montar(
            cabecalho,
            nos,
            livres - diferenca,
            await CustoAsync(personagemId, cancelamento)
        );
        return new ResultadoCarregarPerfil(
            new CarregarPerfilResponse
            {
                Arvore = respostaArvore,
                Avisos = validacao.Avisos.ToArray(),
                CoroasCobradas = cobrado,
            },
            null
        );
    }

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
    };

    private static RankDoNo[] DeserializarRanks(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<RankDoNo>();
        }

        try
        {
            return JsonSerializer.Deserialize<RankDoNo[]>(json, OpcoesJson)
                ?? Array.Empty<RankDoNo>();
        }
        catch
        {
            return Array.Empty<RankDoNo>();
        }
    }

    private static RankDoNo[] Ranks(List<NoComprado> nos) =>
        nos.Where(n => n.Rank > 0)
            .OrderBy(n => n.NoId, StringComparer.Ordinal)
            .Select(n => new RankDoNo { NoId = n.NoId, Rank = n.Rank })
            .ToArray();

    /// <summary>Coroas da redistribuição no nível atual do personagem (a regra é do shared).</summary>
    private async Task<long> CustoAsync(long personagemId, CancellationToken cancelamento) =>
        ConfigRedistribuicao.CustoEmCoroas(
            await pontos.ObterNivelAsync(personagemId, cancelamento)
        );

    private ArvoreResponse Montar(
        ArvoreDoPersonagem? cabecalho,
        List<NoComprado> nos,
        int livres,
        long custoRedistribuicao
    )
    {
        var ranks = Ranks(nos);
        return new ArvoreResponse
        {
            Configuracao = configuracao,
            PontosLivres = livres,
            PontosGastos = cabecalho?.PontosGastos ?? 0,
            CustoRedistribuicaoCoroas = custoRedistribuicao,
            Ranks = ranks,
            Atributos = RegrasDaArvore.SomarAtributos(
                configuracao,
                RegrasDaArvore.ParaDicionario(ranks)
            ),
        };
    }
}
