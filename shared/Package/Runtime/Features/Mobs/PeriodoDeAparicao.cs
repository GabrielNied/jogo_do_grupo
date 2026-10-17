namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Período do dia em que um ponto de spawn ou espécie aparece e repõe (docs/features/mobs.md).
    /// </summary>
    public enum PeriodoDeAparicao : byte
    {
        /// <summary>Aparece e repõe tanto de dia quanto de noite.</summary>
        Sempre = 0,

        /// <summary>Aparece e repõe apenas durante o dia (6h às 22h).</summary>
        Dia = 1,

        /// <summary>Aparece e repõe apenas durante a noite (22h às 6h).</summary>
        Noite = 2,
    }
}
