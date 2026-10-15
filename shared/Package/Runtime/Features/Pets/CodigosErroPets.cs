namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Motivos de recusa em <see cref="PetRecusado.Codigo"/>.</summary>
    public static class CodigosErroPets
    {
        /// <summary>O pet só é chamado na cidade (não em caverna ou dungeon).</summary>
        public const string ForaDaCidade = "ForaDaCidade";

        /// <summary>Não há pet chamado para dispensar.</summary>
        public const string SemPet = "SemPet";
    }
}
