namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Respawn dinâmico pela lotação (#897; proposta do agente, a calibrar): quanto mais jogadores
/// perto do ponto, mais curto o intervalo de reposição, até um limite. O intervalo base é o da
/// ficha da criatura; o teto de vivos da área continua valendo por cima (veja
/// <see cref="LimiteDeInimigosVivos"/>). Valores em <c>docs/features/mobs.md</c>.
/// </summary>
public static class ConfigRespawn
{
    /// <summary>Raio, em metros, em volta do ponto em que um jogador conta para a lotação.</summary>
    public const double RaioDaLotacaoMetros = 60.0;

    /// <summary>Quanto o intervalo encurta por jogador na vizinhança (0,15 = 15%).</summary>
    public const double ReducaoPorJogador = 0.15;

    /// <summary>
    /// Menor fração do intervalo base (0,4 = no máximo 60% mais curto): o limite da curva, que
    /// o 4º jogador já alcança.
    /// </summary>
    public const double FracaoMinimaDoIntervalo = 0.4;

    /// <summary>
    /// Fração do intervalo base para <paramref name="jogadores"/> na vizinhança: 1 sem ninguém,
    /// descendo linearmente até <see cref="FracaoMinimaDoIntervalo"/>.
    /// </summary>
    public static double Fracao(int jogadores) =>
        Math.Clamp(
            1.0 - (ReducaoPorJogador * Math.Max(jogadores, 0)),
            FracaoMinimaDoIntervalo,
            1.0
        );

    /// <summary>Intervalo de reposição, em segundos, dado o da ficha e a lotação.</summary>
    public static double Intervalo(double intervaloBaseSegundos, int jogadores) =>
        intervaloBaseSegundos * Fracao(jogadores);
}
