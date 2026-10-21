namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Intenção do jogador de comprar um pet no cuidador com coroas (PetsMessageIds.ComprarPetCuidador).
    /// </summary>
    [System.Serializable]
    public sealed class ComprarPetCuidador
    {
        public int Especie;

        public ComprarPetCuidador() { }

        public ComprarPetCuidador(int especie)
        {
            Especie = especie;
        }
    }
}
