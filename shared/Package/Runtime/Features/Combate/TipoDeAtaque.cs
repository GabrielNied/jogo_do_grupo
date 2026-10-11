namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Os dois ataques da arma. Os valores são estáveis: nunca mude nem reaproveite um número já
    /// publicado. Regras em docs/features/fatia-jogavel.md, seção 2.
    /// </summary>
    public enum TipoDeAtaque
    {
        /// <summary>Corte rápido, sem custo.</summary>
        Leve = 0,

        /// <summary>Corte lento e mais forte, que gasta vigor.</summary>
        Pesado = 1,
    }
}
