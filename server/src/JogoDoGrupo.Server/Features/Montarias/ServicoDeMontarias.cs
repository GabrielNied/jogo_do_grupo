using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Montarias;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Mundo;
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
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<ServicoDeMontarias>? _logger;

    public ServicoDeMontarias(
        ServidorTempoReal? tempoReal = null,
        IControleDeVelocidadeDoMundo? controleVelocidade = null,
        IEstadoDeCombate? combate = null,
        IPosicoesDoMundo? posicoes = null,
        IVizinhanca? vizinhanca = null,
        IServiceScopeFactory? scopeFactory = null,
        ILogger<ServicoDeMontarias>? logger = null
    )
    {
        _tempoReal = tempoReal;
        _controleVelocidade = controleVelocidade;
        _combate = combate;
        _posicoes = posicoes;
        _vizinhanca = vizinhanca;
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
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        var item = await db.Set<ItemInventario>()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId);

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
        db.Set<ItemInventario>().Remove(item);

        // Registra a montaria na conta
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
