namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Motivos de recusa em <see cref="PetRecusado.Codigo"/>.</summary>
    public static class CodigosErroPets
    {
        /// <summary>O servidor não sabe onde o personagem está (ainda não entrou no mundo).</summary>
        public const string SemPosicao = "SemPosicao";

        /// <summary>Não há pet chamado para dispensar.</summary>
        public const string SemPet = "SemPet";

        /// <summary>Não pode usar item de pet em combate.</summary>
        public const string EmCombate = "EmCombate";

        /// <summary>Item não encontrado na bolsa.</summary>
        public const string ItemNaoEncontrado = "ItemNaoEncontrado";

        /// <summary>Item não é um consumível de pet.</summary>
        public const string ItemNaoEPet = "ItemNaoEPet";

        /// <summary>A conta já possui esta espécie de pet registrada.</summary>
        public const string JaPossuiPet = "JaPossuiPet";

        /// <summary>A espécie de pet ainda não foi desbloqueada na conta.</summary>
        public const string PetNaoDesbloqueado = "PetNaoDesbloqueado";
    }
}
