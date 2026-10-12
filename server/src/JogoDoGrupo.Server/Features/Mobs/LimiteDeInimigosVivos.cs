namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Teto de inimigos comuns vivos ao mesmo tempo, somando todos os grupos (desempenho do laço
/// de IA). Um grupo reserva as vagas antes de reaparecer e devolve uma a cada morte. Seguro
/// entre threads.
/// </summary>
public sealed class LimiteDeInimigosVivos(int maximo)
{
    private int _vivos;

    public int Maximo { get; } = maximo;

    public int Vivos => Volatile.Read(ref _vivos);

    /// <summary>Reserva até <paramref name="desejado"/> vagas e devolve quantas conseguiu.</summary>
    public int Reservar(int desejado)
    {
        while (true)
        {
            var atual = Volatile.Read(ref _vivos);
            var vagas = Math.Clamp(Maximo - atual, 0, Math.Max(desejado, 0));
            if (
                vagas == 0
                || Interlocked.CompareExchange(ref _vivos, atual + vagas, atual) == atual
            )
            {
                return vagas;
            }
        }
    }

    /// <summary>Devolve uma vaga (um inimigo morreu).</summary>
    public void Liberar() => Interlocked.Decrement(ref _vivos);
}
