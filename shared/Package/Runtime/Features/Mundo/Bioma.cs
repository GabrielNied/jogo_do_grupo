namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Biomas do mundo. A cidade fica no meio. Campo, mata e colinas ficam em volta dela.
    /// Deserto, neve, lava e pântano ficam mais longe (docs/features/mundo.md, "Biomas").
    /// </summary>
    public enum Bioma : byte
    {
        /// <summary>A cidade murada, no centro do mundo.</summary>
        Cidade = 0,

        /// <summary>Campo aberto ao norte da cidade.</summary>
        Campo = 1,

        /// <summary>Mata a leste da cidade.</summary>
        Mata = 2,

        /// <summary>Colinas a oeste da cidade.</summary>
        Colinas = 3,

        /// <summary>Deserto de dunas, no oeste do mundo.</summary>
        Deserto = 4,

        /// <summary>Montanhas de neve, no norte do mundo.</summary>
        Neve = 5,

        /// <summary>Montanhas de lava, no sul do mundo.</summary>
        Lava = 6,

        /// <summary>Pântano alagado, no leste do mundo.</summary>
        Pantano = 7,
    }
}
