namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Constantes de balanceamento dos efeitos de status (#171, combate.md, regra 17).
    /// </summary>
    public static class ConfigEfeitosDeStatus
    {
        // ── Durações padrão em segundos ──────────────────────────────────────────
        public const double DuracaoLentidaoSegundos = 4.0;
        public const double DuracaoAtordoadoSegundos = 2.0;
        public const double ImunidadeAtordoadoSegundos = 6.0;
        public const double DuracaoSangramentoSegundos = 5.0;
        public const double DuracaoQueimaduraSegundos = 5.0;
        public const double DuracaoVulneravelSegundos = 5.0;
        public const double DuracaoFortalecidoSegundos = 5.0;
        public const double DuracaoProtegidoSegundos = 5.0;

        // ── Efeitos percentuais ──────────────────────────────────────────────────
        public const double PorcentagemLentidao = 0.30;
        public const double PorcentagemVulneravel = 0.15;
        public const double PorcentagemFortalecido = 0.15;
        public const double PorcentagemProtegido = 0.15;

        // ── DoT (Dano por segundo por acúmulo) ───────────────────────────────────
        public const int DanoPorSegundoSangramento = 5;
        public const int DanoPorSegundoQueimadura = 8;
        public const int MaximoAcumulosDoT = 3;

        // ── Boss: barra de quebra (stagger) ──────────────────────────────────────
        public const int CapacidadeBarraDeQuebraBoss = 100;
        public const int AumentoQuebraPorAtordoamentoBoss = 25;
        public const double DuracaoVulnerabilidadeQuebraBossSegundos = 5.0;
    }
}
