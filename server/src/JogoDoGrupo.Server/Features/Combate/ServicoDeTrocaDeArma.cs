using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A troca de arma no servidor (docs/features/combate.md, "Duas armas"). O client manda a
/// intenção (tecla R); aqui se reserva o personagem por
/// <see cref="RegrasTrocaDeArma.DuracaoDaTrocaSegundos"/> no <see cref="ControleDeAcoes"/> (a mesma
/// reserva da esquiva, sem a janela sem dano), então golpe, esquiva e guarda são recusados nesse
/// tempo. Só depois de reservar se pede a Itens (<see cref="ITrocaDeConjunto"/>) que vire o
/// conjunto. A troca não tem recarga própria nem mexe na recarga das skills, que é de cada arma.
/// </summary>
public sealed class ServicoDeTrocaDeArma : ITrocaDeArma
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly ControleDeAcoes _controle;
    private readonly ITrocaDeConjunto? _conjuntos;
    private readonly ILogger<ServicoDeTrocaDeArma>? _log;
    private readonly Func<long> _relogioMs;
    private readonly ServicoDeQueda? _queda;
    private readonly ConcurrentDictionary<long, long> _fimDaTrocaMs = new();

    public ServicoDeTrocaDeArma(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        ControleDeAcoes controle,
        ITrocaDeConjunto? conjuntos,
        ILogger<ServicoDeTrocaDeArma>? log = null,
        Func<long>? relogioMs = null,
        ServicoDeQueda? queda = null
    )
    {
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _controle = controle;
        _conjuntos = conjuntos;
        _log = log;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _queda = queda;
    }

    /// <summary>Trata um <c>IntencaoDeTrocaDeArma</c> (laço de rede).</summary>
    public void AoTrocar(SessaoTempoReal sessao, IntencaoDeTrocaDeArma intencao)
    {
        var personagemId = sessao.PersonagemId;
        if (_conjuntos == null || !_posicoes.TentarObter(personagemId, out _))
        {
            return; // Fora do mundo: não há arma para trocar.
        }

        var recusa = Reservar(personagemId, out var ocupacaoId);
        if (recusa != null)
        {
            Recusar(sessao, intencao.Sequencia, recusa);
            return;
        }

        _ = ExecutarAsync(sessao, intencao, ocupacaoId);
    }

    /// <inheritdoc />
    public bool EmTroca(long personagemId) =>
        _fimDaTrocaMs.TryGetValue(personagemId, out var fim) && _relogioMs() < fim;

    /// <inheritdoc />
    public string? TentarReservar(long personagemId) =>
        _conjuntos == null || !_posicoes.TentarObter(personagemId, out _)
            ? null
            : Reservar(personagemId, out _);

    // As recusas e a reserva do R e dos botões de conjunto: uma lógica só.
    private string? Reservar(long personagemId, out long ocupacaoId)
    {
        ocupacaoId = 0;

        // Trocar interrompe quem levantava um aliado, como atacar e esquivar.
        _queda?.AoAgir(personagemId);

        if (_queda?.EstaCaido(personagemId) == true)
        {
            return CodigosErroCombate.VoceEstaCaido;
        }

        if (_controle.EstaAtordoado(personagemId))
        {
            return CodigosErroCombate.VoceEstaAtordoado;
        }

        var agora = _relogioMs();
        if (
            !_controle.TentarOcupar(
                personagemId,
                agora,
                RegrasTrocaDeArma.DuracaoMs,
                out ocupacaoId,
                invulneravel: false
            )
        )
        {
            return CodigosErroCombate.AcaoEmAndamento;
        }

        _fimDaTrocaMs[personagemId] = agora + RegrasTrocaDeArma.DuracaoMs;
        return null;
    }

    private void Liberar(long personagemId, long ocupacaoId)
    {
        _controle.Desocupar(personagemId, ocupacaoId);
        _fimDaTrocaMs.TryRemove(personagemId, out _);
    }

    private async Task ExecutarAsync(
        SessaoTempoReal sessao,
        IntencaoDeTrocaDeArma intencao,
        long ocupacaoId
    )
    {
        var personagemId = sessao.PersonagemId;
        try
        {
            var conjunto = await _conjuntos!.AlternarAsync(personagemId, CancellationToken.None);
            if (conjunto is not int ativo)
            {
                Liberar(personagemId, ocupacaoId);
                Recusar(sessao, intencao.Sequencia, CodigosErroCombate.AcaoEmAndamento);
                return;
            }

            _tempoReal.Enviar(
                sessao,
                CombateMessageIds.TrocaDeArmaIniciada,
                new TrocaDeArmaIniciada
                {
                    Conjunto = ativo,
                    DuracaoMs = RegrasTrocaDeArma.DuracaoMs,
                }
            );
        }
        catch (Exception e)
        {
            _log?.LogError(e, "Erro na troca de arma do personagem {Personagem}.", personagemId);
            Liberar(personagemId, ocupacaoId);
            Recusar(sessao, intencao.Sequencia, CodigosErroCombate.AcaoEmAndamento);
        }
    }

    private void Recusar(SessaoTempoReal sessao, int sequencia, string codigo) =>
        _tempoReal.Enviar(
            sessao,
            CombateMessageIds.TrocaDeArmaRecusada,
            new TrocaDeArmaRecusada { Sequencia = sequencia, Codigo = codigo }
        );
}
