namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Resposta do servidor contendo os pets à venda no cuidador (PetsMessageIds.RespostaCuidador).
    /// </summary>
    [System.Serializable]
    public sealed class RespostaCuidador
    {
        public PetCuidadorDto[] Ofertas = new PetCuidadorDto[0];

        public RespostaCuidador() { }

        public RespostaCuidador(PetCuidadorDto[] ofertas)
        {
            Ofertas = ofertas;
        }
    }
}
