namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Posição e orientação de um avatar na cidade.</summary>
public readonly record struct PosicaoNaCidade(float X, float Y, float Z, float RotacaoY);

/// <summary>
/// Regras puras do movimento na cidade (sem rede): o servidor recusa deslocamento mais rápido
/// do que correr. Valores em docs/features/mundo.md.
/// </summary>
public static class ValidadorDeMovimento
{
    /// <summary>Ponto de nascimento do hub (docs/features/mundo.md, "Nascimento").</summary>
    public static readonly PosicaoNaCidade Nascimento = new(0f, 0f, -24f, 0f);

    /// <summary>Velocidade de corrida do client (m/s).</summary>
    public const float VelocidadeMaxima = 5.5f;

    /// <summary>Folga fixa (m) para jitter de rede e de tick.</summary>
    public const float Folga = 0.5f;

    /// <summary>Tempo máximo (ms) que conta para o deslocamento permitido: não acumula ao ficar parado.</summary>
    public const long TempoMaximoMs = 1000;

    /// <summary>Limite do terreno em X e Z (m): 1 km de lado, centrado na origem.</summary>
    public const float LimiteHorizontal = 500f;

    /// <summary>Faixa de altura aceita (m).</summary>
    public const float AlturaMinima = -5f;
    public const float AlturaMaxima = 50f;

    /// <summary>
    /// Confere se ir de <paramref name="atual"/> a <paramref name="desejada"/> em
    /// <paramref name="decorridoMs"/> é possível correndo. O deslocamento é medido no plano
    /// horizontal (queda e pulo não contam).
    /// </summary>
    public static bool Permitido(PosicaoNaCidade atual, PosicaoNaCidade desejada, long decorridoMs)
    {
        if (
            !float.IsFinite(desejada.X)
            || !float.IsFinite(desejada.Y)
            || !float.IsFinite(desejada.Z)
            || !float.IsFinite(desejada.RotacaoY)
        )
        {
            return false;
        }

        if (
            Math.Abs(desejada.X) > LimiteHorizontal
            || Math.Abs(desejada.Z) > LimiteHorizontal
            || desejada.Y < AlturaMinima
            || desejada.Y > AlturaMaxima
        )
        {
            return false;
        }

        var segundos = Math.Clamp(decorridoMs, 0, TempoMaximoMs) / 1000.0;
        var dx = (double)desejada.X - atual.X;
        var dz = (double)desejada.Z - atual.Z;
        var distancia = Math.Sqrt((dx * dx) + (dz * dz));
        return distancia <= (VelocidadeMaxima * segundos) + Folga;
    }
}
