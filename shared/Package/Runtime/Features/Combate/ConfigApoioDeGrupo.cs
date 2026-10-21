namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Valores ajustáveis do escudo e dos buffs de grupo das skills de apoio (#170, combate.md,
    /// regra 17). Valores de teste até o balanceamento final.
    /// </summary>
    public static class ConfigApoioDeGrupo
    {
        /// <summary>Quanto tempo o escudo dura, em segundos (ponto de partida do livro: 8 s).</summary>
        public const double DuracaoDoEscudoSegundos = 8.0;

        /// <summary>Absorção do escudo como fração da vida máxima de quem o recebe.</summary>
        public const double FracaoDoEscudoPelaVidaMaxima = 0.20;

        /// <summary>Quanto tempo dura um buff de grupo vindo de skill, em segundos.</summary>
        public const double DuracaoDoBuffDeGrupoSegundos = 12.0;

        /// <summary>Raio, em metros ao redor de quem usa, dentro do qual os aliados recebem o apoio.</summary>
        public const float AlcanceDoApoioMetros = 12.0f;
    }
}
