namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Roda <see cref="GruposDoServidor.Varrer"/> de tempos em tempos, para os convites expirarem e
/// as vagas guardadas vencerem mesmo quando ninguém mexe no grupo.
/// </summary>
public sealed class VarreduraDosGrupos(GruposDoServidor grupos, ILogger<VarreduraDosGrupos> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(
            TimeSpan.FromMilliseconds(ConfigGrupoServidor.IntervaloDaVarreduraMs)
        );
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    grupos.Varrer();
                }
                catch (Exception e)
                {
                    log.LogError(e, "Erro na varredura dos grupos.");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
