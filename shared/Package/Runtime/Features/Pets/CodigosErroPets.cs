namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Motivos de recusa em <see cref="PetRecusado.Codigo"/>.</summary>
    public static class CodigosErroPets
    {
        /// <summary>O servidor não sabe onde o personagem está (ainda não entrou no mundo).</summary>
        public const string SemPosicao = "SemPosicao";

        /// <summary>Não há pet chamado para dispensar.</summary>
        public const string SemPet = "SemPet";
    }
}
