using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Montarias;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Montarias;

/// <summary>
/// Serviço autoritativo de montarias: gerencia estado montado, velocidade autoritativa
/// no servidor e replicação de estado para clientes próximos.
/// </summary>
public sealed class ServicoDeMontarias : IMontariasConsulta
{
    private readonly ConcurrentDictionary<long, int> _montados = new();
    private readonly ServidorTempoReal? _tempoReal;
    private readonly IControleDeVelocidadeDoMundo? _controleVelocidade;
    private readonly IEstadoDeCombate? _combate;
    private readonly IPosicoesDoMundo? _posicoes;
    private readonly IVizinhanca? _vizinhanca;
    private readonly ILogger<ServicoDeMontarias>? _logger;

    public ServicoDeMontarias(
        ServidorTempoReal? tempoReal = null,
        IControleDeVelocidadeDoMundo? controleVelocidade = null,
        IEstadoDeCombate? combate = null,
        IPosicoesDoMundo? posicoes = null,
        IVizinhanca? vizinhanca = null,
        ILogger<ServicoDeMontarias>? logger = null
    )
    {
        _tempoReal = tempoReal;
        _controleVelocidade = controleVelocidade;
        _combate = combate;
        _posicoes = posicoes;
        _vizinhanca = vizinhanca;
        _logger = logger;
    }

    public bool EstaMontado(long personagemId) => _montados.ContainsKey(personagemId);

    public int? TipoDaMontaria(long personagemId) =>
        _montados.TryGetValue(personagemId, out var tipo) ? tipo : null;

    /// <summary>
    /// Tenta montar na montaria. Recusa se estiver em combate.
    /// </summary>
    public bool ChamarMontaria(
        long personagemId,
        SessaoTempoReal? sessao = null,
        int tipo = RegrasMontaria.TipoPadrao
    )
    {
        if (_combate?.EmCombate(personagemId) == true)
        {
            _logger?.LogInformation(
                "Personagem {PersonagemId} tentou chamar montaria durante o combate (recusado)",
                personagemId
            );
            return false;
        }

        _montados[personagemId] = tipo;
        _controleVelocidade?.DefinirVelocidades(
            personagemId,
            RegrasVelocidade.AndarPadrao,
            RegrasMontaria.VelocidadeMontariaPadrao
        );

        NotificarMudanca(personagemId, montado: true, tipo, sessao);
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
    /// Chamado ao desconectar a sessão para limpar estado sem broadcast.
    /// </summary>
    public void Desconectar(long personagemId)
    {
        _montados.TryRemove(personagemId, out _);
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
