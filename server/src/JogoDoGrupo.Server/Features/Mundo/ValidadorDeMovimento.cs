using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Posição e orientação de um avatar na cidade.</summary>
public readonly record struct PosicaoNaCidade(float X, float Y, float Z, float RotacaoY);

/// <summary>Como o servidor vê um passo do client (ADR 0024).</summary>
public enum JulgamentoDoPasso
{
    /// <summary>Dentro do orçamento: aceita.</summary>
    Normal,

    /// <summary>Um pouco além do orçamento: aceita e conta como suspeita.</summary>
    Suspeito,

    /// <summary>Impossível (ou suspeita demais): o servidor força a posição.</summary>
    Grave,
}

/// <summary>
/// Regras puras do movimento na cidade (sem rede): o servidor recusa deslocamento mais rápido
/// do que correr, para fora do mundo ou longe do chão. Valores em docs/features/mundo.md.
/// </summary>
public static class ValidadorDeMovimento
{
    /// <summary>Ponto de nascimento do hub (docs/features/mundo.md, "Nascimento").</summary>
    public static readonly PosicaoNaCidade Nascimento = new(0f, 0f, -24f, 0f);

    /// <summary>
    /// Velocidade de corrida de quem acabou de entrar (m/s). A de cada personagem é do servidor
    /// e pode mudar (CidadeDoMundo, VelocidadesDeMovimento).
    /// </summary>
    public const float VelocidadeMaxima = RegrasVelocidade.CorrerPadrao;

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
    public static bool Permitido(
        PosicaoNaCidade atual,
        PosicaoNaCidade desejada,
        long decorridoMs,
        float velocidade = VelocidadeMaxima
    )
    {
        if (!PosicaoValida(desejada))
        {
            return false;
        }

        return Distancia(atual, desejada) <= (velocidade * Segundos(decorridoMs)) + Folga;
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

    /// <summary>
    /// Quanto deslocamento (em segundos de corrida) o orçamento junta no máximo (ADR 0024). É o
    /// que absorve pacotes que chegam em rajada: o tempo de rede não vira recusa.
    /// </summary>
    public const double OrcamentoMaximoSegundos = 2.0;

    /// <summary>
    /// Orçamento dado a quem reconecta (s de corrida): o client pode ter andado um pouco enquanto
    /// a conexão estava caída, e isso não pode virar posição forçada.
    /// </summary>
    public const double OrcamentoAoReconectarSegundos = 10.0;

    /// <summary>
    /// Excesso (m) além do orçamento que ainda é aceito, só contando como suspeita. Cobre uma
    /// esquiva (5 m) que o client fez e o servidor recusou: o jogador não volta no tempo.
    /// </summary>
    public const double ToleranciaDeExcesso = 6.0;

    /// <summary>
    /// Suspeitas acumuladas que viram posição forçada. Cada passo suspeito soma 1, e o contador
    /// esvazia <see cref="SuspeitasEsvaziadasPorSegundo"/> por segundo: só abuso repetido passa.
    /// </summary>
    public const double LimiteDeSuspeitas = 5.0;

    public const double SuspeitasEsvaziadasPorSegundo = 0.5;

    /// <summary>Teto do orçamento para a velocidade dada (m).</summary>
    public static double TetoDoOrcamento(float velocidade) =>
        (velocidade * OrcamentoMaximoSegundos) + Folga;

    /// <summary>
    /// Orçamento depois de <paramref name="decorridoMs"/>: soma o que dá para correr nesse tempo,
    /// até o teto. Tempo negativo não soma. Um orçamento já acima do teto (dado na reconexão)
    /// não é cortado, só deixa de crescer.
    /// </summary>
    public static double Recarregar(double orcamento, long decorridoMs, float velocidade)
    {
        var teto = TetoDoOrcamento(velocidade);
        if (orcamento >= teto)
        {
            return orcamento;
        }

        return Math.Min(orcamento + (velocidade * Math.Max(0, decorridoMs) / 1000.0), teto);
    }

    /// <summary>
    /// Julga um passo contra o orçamento. Posição inválida (fora do mundo, longe do chão, não
    /// finita) ou excesso acima de <see cref="ToleranciaDeExcesso"/> é grave; excesso menor é
    /// suspeito, mas aceito.
    /// </summary>
    public static JulgamentoDoPasso Julgar(
        PosicaoNaCidade atual,
        PosicaoNaCidade desejada,
        double orcamento
    )
    {
        if (!PosicaoValida(desejada))
        {
            return JulgamentoDoPasso.Grave;
        }

        var excesso = Distancia(atual, desejada) - orcamento;
        return excesso <= 0 ? JulgamentoDoPasso.Normal
            : excesso <= ToleranciaDeExcesso ? JulgamentoDoPasso.Suspeito
            : JulgamentoDoPasso.Grave;
    }

    /// <summary>Distância no plano horizontal (m) entre duas posições.</summary>
    public static double DistanciaHorizontal(PosicaoNaCidade de, PosicaoNaCidade para) =>
        Distancia(de, para);

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

        if (Math.Abs(desejada.X) > LimiteHorizontal || Math.Abs(desejada.Z) > LimiteHorizontal)
        {
            return false;
        }

        // Dentro do túnel da primeira caverna o chão é o piso dele (abaixo do relevo): vale entre
        // o piso e o teto. Em cima do morro ou dentro da rocha o ponto não é do túnel e cai nas
        // regras do terreno, abaixo.
        if (PrimeiraCaverna.DentroDoTunel(desejada.X, desejada.Z, desejada.Y))
        {
            return true;
        }

        if (desejada.Y < AlturaMinima || desejada.Y > AlturaMaxima)
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
