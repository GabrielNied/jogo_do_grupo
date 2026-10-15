using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Features.Skills;

/// <summary>
/// A parte de rede de Skills: recebe o pedido de uso e o de barra, entrega as respostas e avisa o
/// dono quando a arma muda (<see cref="EquipamentoMudou"/>). Quem decide é o
/// <see cref="NucleoDeSkills"/>. Os manipuladores rodam no laço de rede, então o trabalho (banco e
/// cobrança) vai para tarefas e responde pelo <see cref="ServidorTempoReal"/>.
/// </summary>
public sealed class ServicoDeSkills : IHostedService, IDisposable
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly NucleoDeSkills _nucleo;
    private readonly MotorDePoderes _motor;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly ILogger<ServicoDeSkills> _log;
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeSkills(
        ServidorTempoReal tempoReal,
        NucleoDeSkills nucleo,
        MotorDePoderes motor,
        IPosicoesDoMundo posicoes,
        ILogger<ServicoDeSkills> log
    )
    {
        _tempoReal = tempoReal;
        _nucleo = nucleo;
        _motor = motor;
        _posicoes = posicoes;
        _log = log;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _tempoReal.SessaoEncerrada -= AoEncerrar;
        _parada.Dispose();
    }

    /// <summary>Trata um <c>UsarSkill</c> (laço de rede).</summary>
    public void AoUsar(SessaoTempoReal sessao, UsarSkill pedido) =>
        _ = ExecutarAsync(
            sessao,
            async () =>
            {
                var resultado = await _nucleo.UsarAsync(sessao.PersonagemId, pedido, _parada.Token);
                if (resultado.Recusa != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        SkillsMessageIds.SkillRecusada,
                        new SkillRecusada
                        {
                            Sequencia = pedido.Sequencia,
                            Espaco = pedido.Espaco,
                            Codigo = resultado.Recusa,
                        }
                    );
                    return;
                }

                if (resultado.Recursos != null)
                {
                    // A mensagem é de Combate: as barras de vida, stamina e mana dele a mostram.
                    _tempoReal.Enviar(
                        sessao,
                        CombateMessageIds.RecursosMudaram,
                        resultado.Recursos
                    );
                }

                _tempoReal.Enviar(sessao, SkillsMessageIds.SkillUsada, resultado.Usada);
                if (resultado.Acertos is { Acertos.Length: > 0 })
                {
                    EnviarParaArea(resultado.Area, sessao.PersonagemId, resultado.Acertos);
                }

                EnviarAvisos(sessao, resultado.Poderes);
                if (resultado.BarraMudou)
                {
                    // Um poder tirou recarga de outra skill: o dono recebe os tempos novos.
                    await EnviarBarraAsync(sessao);
                }
            }
        );

    /// <summary>Trata um <c>PedirBarraDeSkills</c> (laço de rede).</summary>
    public void AoPedirBarra(SessaoTempoReal sessao) =>
        _ = ExecutarAsync(sessao, () => EnviarBarraAsync(sessao));

    /// <summary>Itens avisou que o equipamento mudou: a barra do dono pode ter mudado.</summary>
    public void AoEquipamentoMudar(EquipamentoMudou evento)
    {
        // Os poderes e os marcos de set dependem do que está vestido: recalcula na próxima vez.
        _motor.Invalidar(evento.PersonagemId);
        if (_tempoReal.TentarObterSessao(evento.PersonagemId, out var sessao) && sessao != null)
        {
            _ = ExecutarAsync(sessao, () => EnviarBarraAsync(sessao));
        }
    }

    /// <summary>Combate avisou que um golpe comum do personagem acertou.</summary>
    public void AoGolpeComum(GolpeComumAcertou evento) =>
        AoGatilhoDeCombate(
            evento.PersonagemId,
            () =>
                _motor.AoGolpeComumAsync(
                    evento.PersonagemId,
                    evento.AlvoIds.ToArray(),
                    _parada.Token
                )
        );

    /// <summary>Combate avisou que o personagem esquivou.</summary>
    public void AoEsquivar(PersonagemEsquivou evento) =>
        AoGatilhoDeCombate(
            evento.PersonagemId,
            () => _motor.AoEsquivarAsync(evento.PersonagemId, _parada.Token)
        );

    // Os gatilhos de Combate vêm do laço de rede: o trabalho vai para uma tarefa.
    private void AoGatilhoDeCombate(long personagemId, Func<Task<ResultadoDosPoderes>> avaliar)
    {
        if (!_tempoReal.TentarObterSessao(personagemId, out var sessao) || sessao == null)
        {
            return;
        }

        _ = ExecutarAsync(
            sessao,
            async () =>
            {
                var resultado = await avaliar();
                EnviarAvisos(sessao, resultado.Avisos);
            }
        );
    }

    private void EnviarAvisos(SessaoTempoReal sessao, IReadOnlyList<PoderDisparou>? avisos)
    {
        if (avisos == null)
        {
            return;
        }

        foreach (var aviso in avisos)
        {
            _tempoReal.Enviar(sessao, SkillsMessageIds.PoderDisparou, aviso);
        }
    }

    private async Task EnviarBarraAsync(SessaoTempoReal sessao)
    {
        var barra = await _nucleo.BarraAsync(sessao.PersonagemId, _parada.Token);
        _tempoReal.Enviar(sessao, SkillsMessageIds.BarraDeSkillsMudou, barra);
    }

    private async Task ExecutarAsync(SessaoTempoReal sessao, Func<Task> trabalho)
    {
        try
        {
            await trabalho();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro em skills do personagem {Personagem}.", sessao.PersonagemId);
        }
    }

    // Quem está na área (e o próprio dono, mesmo se acabou de sair dela).
    private void EnviarParaArea(string area, long donoId, SkillAcertou acertou)
    {
        var presentes = new HashSet<long>(_posicoes.PersonagensNaArea(area)) { donoId };
        foreach (var sessao in _tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                _tempoReal.Enviar(sessao, SkillsMessageIds.SkillAcertou, acertou);
            }
        }
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        _nucleo.Esquecer(sessao.PersonagemId);
        _motor.Esquecer(sessao.PersonagemId);
    }
}
