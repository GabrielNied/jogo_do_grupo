namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// O que a criatura é no mundo. Os valores são estáveis: nunca mude nem reaproveite um
    /// número já publicado.
    /// </summary>
    public enum CategoriaDeCriatura
    {
        /// <summary>Inimigo comum. É a única categoria que pode vir fortalecida.</summary>
        Comum = 0,

        /// <summary>Chefe: sempre aparece, sempre com a mesma força.</summary>
        Chefe = 1,

        /// <summary>Chefe raro: força fixa como a do chefe, mas pode aparecer ou não.</summary>
        ChefeRaro = 2,
    }
}
