using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Posição e orientação de um avatar na cidade.</summary>
public readonly record struct PosicaoNaCidade(float X, float Y, float Z, float RotacaoY);

/// <summary>
/// Regras puras do movimento na cidade (sem rede): o servidor recusa deslocamento mais rápido
/// do que correr, para fora do mundo ou longe do chão. Valores em docs/features/mundo.md.
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

    /// <summary>
    /// Folga (m) no limite de distância de uma janela de deslocamento: só cobre o arredondamento
    /// dos números (5 m passa, 5,1 m não).
    /// </summary>
    public const double ToleranciaDaJanela = 0.01;

    /// <summary>
    /// Tempo a mais (ms) que uma janela de deslocamento fica aberta depois da duração pedida,
    /// para a última posição do deslocamento não ser recusada por atraso de rede.
    /// </summary>
    public const long FolgaDaJanelaMs = 150;

    /// <summary>
    /// Limite do terreno em X e Z (m): metade do lado do mundo, centrado na origem. Era 500 m
    /// (terreno de 1 km) até 2026-10-11; o mundo passou a 3 km.
    /// </summary>
    public const float LimiteHorizontal = MapaDoMundo.Metade;

    /// <summary>
    /// Faixa de altura aceita em qualquer ponto (m). Era de -5 a 50 m enquanto o chão era plano;
    /// agora acompanha o relevo, e a conferência fina é contra o chão do ponto (abaixo).
    /// </summary>
    public const float AlturaMinima = MapaDoMundo.AlturaMinima - FolgaAbaixoDoChao;
    public const float AlturaMaxima = MapaDoMundo.AlturaMaxima + FolgaAcimaDoChao;

    /// <summary>Quanto o avatar pode ficar abaixo do chão do ponto (m): folga de encosta.</summary>
    public const float FolgaAbaixoDoChao = 3f;

    /// <summary>
    /// Quanto o avatar pode ficar acima do chão do ponto (m): pulo e queda de penhasco (o maior
    /// paredão do mapa tem uns 70 m).
    /// </summary>
    public const float FolgaAcimaDoChao = 90f;

    /// <summary>
    /// Confere se ir de <paramref name="atual"/> a <paramref name="desejada"/> em
    /// <paramref name="decorridoMs"/> é possível correndo. O deslocamento é medido no plano
    /// horizontal (queda e pulo não contam).
    /// </summary>
    public static bool Permitido(PosicaoNaCidade atual, PosicaoNaCidade desejada, long decorridoMs)
    {
        if (!PosicaoValida(desejada))
        {
            return false;
        }

        return Distancia(atual, desejada) <= (VelocidadeMaxima * Segundos(decorridoMs)) + Folga;
    }

    /// <summary>
    /// O mesmo de <see cref="Permitido"/>, mas dentro de uma janela de deslocamento (ex.: a
    /// esquiva): o passo pode ir a <paramref name="velocidade"/> (m/s), e a posição não pode
    /// ficar a mais de <paramref name="distanciaMaxima"/> (m) de <paramref name="origem"/>, que
    /// é onde o personagem estava quando a janela abriu.
    /// </summary>
    public static bool PermitidoNaJanela(
        PosicaoNaCidade origem,
        PosicaoNaCidade atual,
        PosicaoNaCidade desejada,
        long decorridoMs,
        double distanciaMaxima,
        double velocidade
    )
    {
        if (!PosicaoValida(desejada))
        {
            return false;
        }

        if (Distancia(origem, desejada) > distanciaMaxima + ToleranciaDaJanela)
        {
            return false;
        }

        return Distancia(atual, desejada) <= (velocidade * Segundos(decorridoMs)) + Folga;
    }

    // Números finitos, dentro do mundo e perto do chão daquele ponto.
    private static bool PosicaoValida(PosicaoNaCidade desejada)
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

        // O chão tem relevo: a altura pedida precisa estar perto do chão daquele ponto.
        var chao = MapaDoMundo.AlturaEm(desejada.X, desejada.Z);
        return desejada.Y >= chao - FolgaAbaixoDoChao && desejada.Y <= chao + FolgaAcimaDoChao;
    }

    private static double Segundos(long decorridoMs) =>
        Math.Clamp(decorridoMs, 0, TempoMaximoMs) / 1000.0;

    // Deslocamento no plano horizontal (queda e pulo não contam).
    private static double Distancia(PosicaoNaCidade de, PosicaoNaCidade para)
    {
        var dx = (double)para.X - de.X;
        var dz = (double)para.Z - de.Z;
        return Math.Sqrt((dx * dx) + (dz * dz));
    }
}
