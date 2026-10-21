using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Montarias;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Montarias;

/// <summary>
/// Serviço autoritativo de montarias: gerencia estado montado, velocidade autoritativa
/// no servidor, desbloqueio por item e replicação de estado para clientes próximos.
/// </summary>
public sealed class ServicoDeMontarias : IMontariasConsulta
{
    private readonly ConcurrentDictionary<long, int> _montados = new();
    private readonly ConcurrentDictionary<long, int> _montariasEquipadas = new();
    private readonly ConcurrentDictionary<long, HashSet<int>> _colecoesPorConta = new();
    private readonly ServidorTempoReal? _tempoReal;
    private readonly IControleDeVelocidadeDoMundo? _controleVelocidade;
    private readonly IEstadoDeCombate? _combate;
    private readonly IPosicoesDoMundo? _posicoes;
    private readonly IVizinhanca? _vizinhanca;
    private readonly ICarteiraCoroas? _carteiraCoroas;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<ServicoDeMontarias>? _logger;

    public ServicoDeMontarias(
        ServidorTempoReal? tempoReal = null,
        IControleDeVelocidadeDoMundo? controleVelocidade = null,
        IEstadoDeCombate? combate = null,
        IPosicoesDoMundo? posicoes = null,
        IVizinhanca? vizinhanca = null,
        ICarteiraCoroas? carteiraCoroas = null,
        IServiceScopeFactory? scopeFactory = null,
        ILogger<ServicoDeMontarias>? logger = null
    )
    {
        _tempoReal = tempoReal;
        _controleVelocidade = controleVelocidade;
        _combate = combate;
        _posicoes = posicoes;
        _vizinhanca = vizinhanca;
        _carteiraCoroas = carteiraCoroas;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool EstaMontado(long personagemId) => _montados.ContainsKey(personagemId);

    public int? TipoDaMontaria(long personagemId) =>
        _montados.TryGetValue(personagemId, out var tipo) ? tipo : null;

    public int ObterMontariaEquipada(long personagemId) =>
        _montariasEquipadas.TryGetValue(personagemId, out var tipo)
            ? tipo
            : RegrasMontaria.TipoPadrao;

    /// <summary>
    /// Consulta as montarias desbloqueadas pela conta.
    /// </summary>
    public IReadOnlyCollection<int> ObterColecao(long contaId)
    {
        if (_colecoesPorConta.TryGetValue(contaId, out var cache))
        {
            lock (cache)
            {
                return cache.ToArray();
            }
        }

        if (_scopeFactory != null && contaId > 0)
        {
            using var escopo = _scopeFactory.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var desbloqueadas = db.Set<MontariaDaConta>()
                .Where(m => m.ContaId == contaId)
                .Select(m => m.TipoMontaria)
                .ToList();

            var conjunto = new HashSet<int>(desbloqueadas) { RegrasMontaria.TipoCastanho };
            _colecoesPorConta[contaId] = conjunto;
            return conjunto.ToArray();
        }

        return new[] { RegrasMontaria.TipoCastanho };
    }

    public bool PossuiMontaria(long contaId, int tipoMontaria)
    {
        if (RegrasMontaria.DesbloqueadaPorPadrao(tipoMontaria))
        {
            return true;
        }

        if (contaId <= 0)
        {
            return false;
        }

        return ObterColecao(contaId).Contains(tipoMontaria);
    }

    /// <summary>
    /// Equipa uma montaria da coleção para ser chamada pelo personagem.
    /// Se já estiver montado, atualiza o visual da montaria imediatamente.
    /// </summary>
    public bool EquiparMontaria(long personagemId, int tipo, SessaoTempoReal? sessao = null)
    {
        if (!RegrasMontaria.TipoValido(tipo))
        {
            return false;
        }

        if (sessao != null && sessao.ContaId > 0 && !PossuiMontaria(sessao.ContaId, tipo))
        {
            _tempoReal?.Enviar(
                sessao,
                MontariasMessageIds.MontariaRecusada,
                new MontariaRecusada
                {
                    Codigo = CodigosErroMontarias.MontariaNaoDesbloqueada,
                    Motivo = "Você não possui esta montaria desbloqueada em sua conta.",
                }
            );
            return false;
        }

        _montariasEquipadas[personagemId] = tipo;

        // Se estiver montado no momento, troca a montaria ativa na hora
        if (_montados.ContainsKey(personagemId))
        {
            _montados[personagemId] = tipo;
            NotificarMudanca(personagemId, montado: true, tipo, sessao);
        }

        if (sessao != null && _tempoReal != null)
        {
            _tempoReal.Enviar(
                sessao,
                MontariasMessageIds.MontariaEquipada,
                new MontariaEquipada { TipoMontaria = tipo }
            );
        }

        return true;
    }

    /// <summary>
    /// Tenta montar na montaria equipada ou indicada. Recusa se estiver em combate.
    /// </summary>
    public bool ChamarMontaria(long personagemId, SessaoTempoReal? sessao = null, int? tipo = null)
    {
        if (_combate?.EmCombate(personagemId) == true)
        {
            _logger?.LogInformation(
                "Personagem {PersonagemId} tentou chamar montaria durante o combate (recusado)",
                personagemId
            );
            return false;
        }

        var tipoFinal = tipo ?? ObterMontariaEquipada(personagemId);
        if (!RegrasMontaria.TipoValido(tipoFinal))
        {
            tipoFinal = RegrasMontaria.TipoPadrao;
        }

        _montados[personagemId] = tipoFinal;
        _controleVelocidade?.DefinirVelocidades(
            personagemId,
            RegrasVelocidade.AndarPadrao,
            RegrasMontaria.VelocidadeMontariaPadrao
        );

        NotificarMudanca(personagemId, montado: true, tipoFinal, sessao);
        return true;
    }

    /// <summary>
    /// Desmonta o personagem e restaura as velocidades de corrida padrão a pé.
    /// </summary>
    public bool Desmontar(long personagemId, SessaoTempoReal? sessao = null)
    {
        if (!_montados.TryRemove(personagemId, out _))
        {
            return false;
        }

        _controleVelocidade?.DefinirVelocidades(
            personagemId,
            RegrasVelocidade.AndarPadrao,
            RegrasVelocidade.CorrerPadrao
        );

        NotificarMudanca(personagemId, montado: false, tipo: 0, sessao);
        return true;
    }

    /// <summary>
    /// Usa um item consumível de montaria da bolsa do jogador, registrando-a permanentemente na conta.
    /// </summary>
    public async Task<bool> UsarItemMontariaAsync(
        long contaId,
        long personagemId,
        long instanciaId,
        SessaoTempoReal? sessao = null
    )
    {
        if (_combate?.EmCombate(personagemId) == true)
        {
            if (sessao != null && _tempoReal != null)
            {
                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.MontariaRecusada,
                    new MontariaRecusada
                    {
                        Codigo = CodigosErroMontarias.EmCombate,
                        Motivo = "Não é possível usar item de montaria em combate.",
                    }
                );
            }
            return false;
        }

        if (_scopeFactory == null)
        {
            return false;
        }

        using var escopo = _scopeFactory.CreateScope();
        var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();

        var item = await custodia.ObterAsync(personagemId, instanciaId, CancellationToken.None);

        if (item == null)
        {
            if (sessao != null && _tempoReal != null)
            {
                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.MontariaRecusada,
                    new MontariaRecusada
                    {
                        Codigo = CodigosErroMontarias.ItemNaoEncontrado,
                        Motivo = "Item não encontrado no inventário.",
                    }
                );
            }
            return false;
        }

        var tipoMontaria = RegrasMontaria.TipoMontariaDoItem(item.ItemId);
        if (tipoMontaria == 0)
        {
            if (sessao != null && _tempoReal != null)
            {
                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.MontariaRecusada,
                    new MontariaRecusada
                    {
                        Codigo = CodigosErroMontarias.ItemNaoEMontaria,
                        Motivo = "Este item não é uma montaria consumível.",
                    }
                );
            }
            return false;
        }

        if (PossuiMontaria(contaId, tipoMontaria))
        {
            if (sessao != null && _tempoReal != null)
            {
                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.MontariaRecusada,
                    new MontariaRecusada
                    {
                        Codigo = CodigosErroMontarias.JaPossuiMontaria,
                        Motivo = "Você já possui esta montaria registrada em sua conta.",
                    }
                );
            }
            return false;
        }

        // Consome o item da bolsa
        var retirado = await custodia.RetirarAsync(
            personagemId,
            instanciaId,
            CancellationToken.None
        );
        if (retirado == null)
        {
            return false;
        }

        // Registra a montaria na conta
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var registro = new MontariaDaConta
        {
            ContaId = contaId,
            TipoMontaria = tipoMontaria,
            DesbloqueadaEm = DateTime.UtcNow,
        };
        db.Set<MontariaDaConta>().Add(registro);
        await db.SaveChangesAsync();

        // Atualiza cache em memória
        var colecao = _colecoesPorConta.GetOrAdd(
            contaId,
            _ => new HashSet<int> { RegrasMontaria.TipoCastanho }
        );
        lock (colecao)
        {
            colecao.Add(tipoMontaria);
        }

        if (sessao != null && _tempoReal != null)
        {
            _tempoReal.Enviar(
                sessao,
                MontariasMessageIds.MontariaDesbloqueada,
                new MontariaDesbloqueada
                {
                    TipoMontaria = tipoMontaria,
                    Nome = RegrasMontaria.ObterNome(tipoMontaria),
                }
            );

            EnviarColecao(sessao);
        }

        EquiparMontaria(personagemId, tipoMontaria, sessao);
        return true;
    }

    /// <summary>
    /// Compra uma montaria básica no estábulo da cidade com coroas, garantindo atomicidade e proximidade (#1067).
    /// </summary>
    public async Task<(
        bool Sucesso,
        string? CodigoErro,
        string? Mensagem,
        long SaldoRestante
    )> ComprarMontariaEstabuloAsync(
        long personagemId,
        int tipoMontaria,
        SessaoTempoReal? sessao = null,
        CancellationToken ct = default
    )
    {
        if (!ConfigEstabulo.EstaAVenda(tipoMontaria))
        {
            var erro = CodigosErroMontarias.MontariaNaoAVenda;
            var msg = "Esta montaria não está à venda no estábulo.";
            NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
            return (false, erro, msg, 0);
        }

        if (_combate?.EmCombate(personagemId) == true)
        {
            var erro = CodigosErroMontarias.EmCombate;
            var msg = "Não é possível comprar montarias durante o combate.";
            NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
            return (false, erro, msg, 0);
        }

        if (_posicoes != null && _posicoes.TentarObter(personagemId, out var pos))
        {
            var npc = CatalogoNpcs.Cavalarico;
            if (pos.Area != npc.Area || !npc.Alcanca(pos.X, pos.Z))
            {
                var erro = CodigosErroMontarias.LongeDoNpc;
                var msg = "Você está muito longe do estábulo para comprar montarias.";
                NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                return (false, erro, msg, 0);
            }
        }

        long contaId = sessao?.ContaId ?? 0;

        if (_scopeFactory == null && _carteiraCoroas == null)
        {
            return (false, CodigosErroMontarias.MontariaNaoAVenda, "Serviço indisponível.", 0);
        }

        if (_scopeFactory != null)
        {
            using var escopo = _scopeFactory.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var carteira = _carteiraCoroas ?? escopo.ServiceProvider.GetService<ICarteiraCoroas>();

            if (contaId <= 0)
            {
                var p = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == personagemId, ct);
                if (p != null)
                {
                    contaId = p.ContaId;
                }
            }

            if (contaId > 0 && PossuiMontaria(contaId, tipoMontaria))
            {
                var erro = CodigosErroMontarias.JaPossuiMontaria;
                var msg = "Você já possui esta montaria registrada em sua conta.";
                NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                return (false, erro, msg, 0);
            }

            var preco = ConfigEstabulo.ObterPreco(tipoMontaria);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var jaTemNoBanco = await db.Set<MontariaDaConta>()
                .AnyAsync(m => m.ContaId == contaId && m.TipoMontaria == tipoMontaria, ct);
            if (jaTemNoBanco)
            {
                var erro = CodigosErroMontarias.JaPossuiMontaria;
                var msg = "Você já possui esta montaria registrada em sua conta.";
                NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                return (false, erro, msg, 0);
            }

            if (carteira != null)
            {
                var debitou = await carteira.DebitarAsync(personagemId, preco, ct);
                if (!debitou)
                {
                    var erro = CodigosErroMontarias.SaldoInsuficiente;
                    var msg = "Você não possui coroas suficientes para comprar esta montaria.";
                    NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                    return (false, erro, msg, 0);
                }
            }

            var registro = new MontariaDaConta
            {
                ContaId = contaId,
                TipoMontaria = tipoMontaria,
                DesbloqueadaEm = DateTime.UtcNow,
            };
            db.Set<MontariaDaConta>().Add(registro);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var colecao = _colecoesPorConta.GetOrAdd(
                contaId,
                _ => new HashSet<int> { RegrasMontaria.TipoCastanho }
            );
            lock (colecao)
            {
                colecao.Add(tipoMontaria);
            }

            long saldoRestante = carteira != null ? await carteira.SaldoAsync(personagemId, ct) : 0;

            if (sessao != null && _tempoReal != null)
            {
                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.MontariaDesbloqueada,
                    new MontariaDesbloqueada
                    {
                        TipoMontaria = tipoMontaria,
                        Nome = RegrasMontaria.ObterNome(tipoMontaria),
                    }
                );

                EnviarColecao(sessao);

                _tempoReal.Enviar(
                    sessao,
                    MontariasMessageIds.RespostaCompraEstabulo,
                    new RespostaCompraEstabulo(
                        sucesso: true,
                        tipoMontaria: tipoMontaria,
                        saldoRestante: saldoRestante
                    )
                );
            }

            EquiparMontaria(personagemId, tipoMontaria, sessao);
            return (true, null, null, saldoRestante);
        }
        else
        {
            if (contaId > 0 && PossuiMontaria(contaId, tipoMontaria))
            {
                var erro = CodigosErroMontarias.JaPossuiMontaria;
                var msg = "Você já possui esta montaria registrada em sua conta.";
                NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                return (false, erro, msg, 0);
            }

            var preco = ConfigEstabulo.ObterPreco(tipoMontaria);
            if (_carteiraCoroas != null)
            {
                var debitou = await _carteiraCoroas.DebitarAsync(personagemId, preco, ct);
                if (!debitou)
                {
                    var erro = CodigosErroMontarias.SaldoInsuficiente;
                    var msg = "Você não possui coroas suficientes para comprar esta montaria.";
                    NotificarRecusaCompra(sessao, tipoMontaria, erro, msg);
                    return (false, erro, msg, 0);
                }
            }

            var colecao = _colecoesPorConta.GetOrAdd(
                contaId,
                _ => new HashSet<int> { RegrasMontaria.TipoCastanho }
            );
            lock (colecao)
            {
                colecao.Add(tipoMontaria);
            }

            long saldoRestante =
                _carteiraCoroas != null ? await _carteiraCoroas.SaldoAsync(personagemId, ct) : 0;
            return (true, null, null, saldoRestante);
        }
    }

    private void NotificarRecusaCompra(
        SessaoTempoReal? sessao,
        int tipoMontaria,
        string codigo,
        string motivo
    )
    {
        if (sessao != null && _tempoReal != null)
        {
            _tempoReal.Enviar(
                sessao,
                MontariasMessageIds.RespostaCompraEstabulo,
                new RespostaCompraEstabulo(
                    sucesso: false,
                    tipoMontaria: tipoMontaria,
                    saldoRestante: 0,
                    codigoErro: codigo,
                    mensagem: motivo
                )
            );
        }
    }

    /// <summary>
    /// Envia a coleção de montarias desbloqueadas para a sessão do jogador.
    /// </summary>
    public void EnviarColecao(SessaoTempoReal sessao)
    {
        if (_tempoReal == null || sessao == null)
        {
            return;
        }

        var colecao = ObterColecao(sessao.ContaId);
        _tempoReal.Enviar(
            sessao,
            MontariasMessageIds.ColecaoMontarias,
            new ColecaoMontarias { MontariasDesbloqueadas = colecao.ToArray() }
        );
    }

    /// <summary>
    /// Chamado ao desconectar a sessão para limpar estado sem broadcast.
    /// </summary>
    public void Desconectar(long personagemId)
    {
        _montados.TryRemove(personagemId, out _);
        _montariasEquipadas.TryRemove(personagemId, out _);
    }

    private void NotificarMudanca(
        long personagemId,
        bool montado,
        int tipo,
        SessaoTempoReal? sessaoOrigem
    )
    {
        var mensagem = new EstadoMontaria
        {
            PersonagemId = personagemId,
            Montado = montado,
            TipoMontaria = tipo,
        };

        if (sessaoOrigem != null && _tempoReal != null)
        {
            _tempoReal.Enviar(sessaoOrigem, MontariasMessageIds.EstadoMontaria, mensagem);
        }

        if (_tempoReal == null)
        {
            return;
        }

        HashSet<long>? recipientes = null;
        if (_posicoes != null && _posicoes.TentarObter(personagemId, out var pos))
        {
            var quemVe =
                (pos.Area == AreasDoMundo.Cidade && _vizinhanca != null)
                    ? _vizinhanca.QuemVe(personagemId)
                    : _posicoes.PersonagensNaArea(pos.Area);
            recipientes = new HashSet<long>(quemVe);
        }

        foreach (var s in _tempoReal.Sessoes)
        {
            if (s.PersonagemId == personagemId && sessaoOrigem != null)
            {
                continue;
            }

            if (
                recipientes == null
                || recipientes.Contains(s.PersonagemId)
                || s.PersonagemId == personagemId
            )
            {
                _tempoReal.Enviar(s, MontariasMessageIds.EstadoMontaria, mensagem);
            }
        }
    }
}
