namespace JogoDoGrupo.Client.Features.Mundo.DiaENoite
{
    /// <summary>
    /// Valores ajustáveis da aparência do dia e da noite no client (issue #382). Só aparência: a
    /// hora e o que é noite vêm do servidor e de <c>RegrasDiaENoite</c> (shared).
    /// </summary>
    public static class ConfigDiaENoite
    {
        // ----- Crepúsculo -----

        /// <summary>
        /// Horas que a troca entre dia e noite leva, centradas no início e no fim da noite (das
        /// 19h às 21h e das 4h às 6h). As lanternas mudam no meio da troca.
        /// </summary>
        public const float DuracaoDoCrepusculoHoras = 2f;

        // ----- Quanto a noite escurece (0 = nada, 1 = preto) -----

        /// <summary>Céu e luz ambiente de cima.</summary>
        public const float EscurecimentoDoCeu = 0.72f;

        /// <summary>Luz ambiente dos lados.</summary>
        public const float EscurecimentoDoHorizonte = 0.55f;

        /// <summary>Névoa. Menos que o céu, para o horizonte não sumir.</summary>
        public const float EscurecimentoDaNevoa = 0.6f;

        /// <summary>Quanto a cor do sol vira a cor fria da lua.</summary>
        public const float MisturaDaLua = 0.7f;

        /// <summary>Força da luz à noite, em relação à do dia (a lua é fraca).</summary>
        public const float ForcaDaLua = 0.15f;

        // ----- Caminho da luz principal -----

        /// <summary>Altura mínima da luz, em graus, ao nascer e ao se pôr (sombra longa sem sumir).</summary>
        public const float AlturaMinima = 8f;

        /// <summary>Altura máxima da lua, em graus, no meio da noite.</summary>
        public const float AlturaDaLua = 35f;

        /// <summary>
        /// Quantos graus a luz gira no céu durante o dia (e de novo durante a noite). Com 180 o sol
        /// se põe onde a lua nasce, e o giro não pula.
        /// </summary>
        public const float VarreduraDoCeu = 180f;
    }
}
