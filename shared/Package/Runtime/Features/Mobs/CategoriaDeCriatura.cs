namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// O que a criatura é no mundo. Os valores são estáveis: nunca mude nem reaproveite um
    /// número já publicado.
    /// </summary>
    public enum CategoriaDeCriatura
    {
        /// <summary>Inimigo comum. É a única categoria que pode vir elite.</summary>
        Comum = 0,

        /// <summary>Chefe: sempre aparece, sempre com a mesma força.</summary>
        Chefe = 1,

        /// <summary>Chefe raro: força fixa como a do chefe, mas pode aparecer ou não.</summary>
        ChefeRaro = 2,

        /// <summary>
        /// Chefe de mundo: força fixa, aparece em lugar e hora sorteados, avisa na área toda e
        /// fica até morrer (sem sumiço por tempo; isso é do chefe raro). Ids de 300 a 399.
        /// </summary>
        ChefeDeMundo = 3,
    }
}
