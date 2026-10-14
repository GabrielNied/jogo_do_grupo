namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Customização visual básica do personagem na criação (tom de pele, cabelo e cor de cabelo).
    /// </summary>
    [System.Serializable]
    public class AparenciaPersonagem
    {
        /// <summary>Índice do tom de pele (0 a <see cref="RegrasPersonagem.TotalTonsDePele"/> - 1).</summary>
        public int TomDePele;

        /// <summary>Índice do estilo de cabelo (0 a <see cref="RegrasPersonagem.TotalCabelos"/> - 1).</summary>
        public int Cabelo;

        /// <summary>Índice da cor de cabelo (0 a <see cref="RegrasPersonagem.TotalCoresDeCabelo"/> - 1).</summary>
        public int CorDeCabelo;
    }
}
