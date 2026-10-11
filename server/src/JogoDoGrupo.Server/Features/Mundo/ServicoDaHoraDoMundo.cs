using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Manda a hora do mundo: a quem entra na área, na hora, e a todos os presentes a cada
/// <see cref="ConfigDiaENoiteServidor.IntervaloDeSincroniaSegundos"/>. A hora é sempre a do
/// <see cref="RelogioDoMundo"/>, igual para todos.
/// </summary>
public sealed class ServicoDaHoraDoMundo(
    RelogioDoMundo relogio,
    ServidorTempoReal tempoReal,
    Func<IReadOnlyList<SessaoTempoReal>> presentes,
    ILogger<ServicoDaHoraDoMundo> log
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var sincronia = new PeriodicTimer(
            TimeSpan.FromSeconds(ConfigDiaENoiteServidor.IntervaloDeSincroniaSegundos)
        );
        try
        {
            while (await sincronia.WaitForNextTickAsync(parada))
            {
                try
                {
                    Sincronizar();
                }
                catch (Exception erro)
                {
                    log.LogError(erro, "Erro ao enviar a hora do mundo.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>Manda a hora de agora a uma sessão (quem acabou de entrar na área).</summary>
    public void Enviar(SessaoTempoReal sessao) =>
        tempoReal.Enviar(sessao, MundoMessageIds.HoraDoMundo, relogio.Mensagem());

    /// <summary>
    /// Manda a mesma hora a todos os presentes. O laço chama no intervalo; os testes chamam
    /// direto.
    /// </summary>
    public void Sincronizar()
    {
        var hora = relogio.Mensagem();
        foreach (var sessao in presentes())
        {
            tempoReal.Enviar(sessao, MundoMessageIds.HoraDoMundo, hora);
        }
    }
}
