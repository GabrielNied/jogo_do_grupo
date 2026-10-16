namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Como a criatura veio ao aparecer. Os valores são estáveis: nunca mude nem reaproveite um
    /// número já publicado.
    /// </summary>
    public enum VarianteDeCriatura
    {
        Normal = 0,

        /// <summary>
        /// Mais forte, maior e com aura vermelha; dá mais recompensa. Só criaturas comuns
        /// (veja <see cref="RegrasCriaturas"/>).
        /// </summary>
        Elite = 1,
    }
}
