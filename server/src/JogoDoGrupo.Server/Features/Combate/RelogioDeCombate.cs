namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O relógio do combate: o "agora" em milissegundos e a espera até um instante. Em produção é o
/// tempo real; os testes trocam por um relógio que só anda quando o teste manda, para provar as
/// janelas (preparo, recuperação, queda) sem dormir de verdade.
/// </summary>
public class RelogioDeCombate
{
    public virtual long AgoraMs => Environment.TickCount64;

    /// <summary>Espera até <paramref name="instanteMs"/> (na escala de <see cref="AgoraMs"/>).</summary>
    public virtual Task EsperarAteAsync(long instanteMs, CancellationToken cancelamento)
    {
        var falta = instanteMs - AgoraMs;
        return falta > 0
            ? Task.Delay(TimeSpan.FromMilliseconds(falta), cancelamento)
            : Task.CompletedTask;
    }
}
