namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Resposta do servidor contendo os pets à venda no cuidador (PetsMessageIds.RespostaCuidador).
    /// </summary>
    public sealed class RespostaCuidador
    {
        public PetCuidadorDto[] Ofertas { get; set; } = new PetCuidadorDto[0];

        public RespostaCuidador() { }

        public RespostaCuidador(PetCuidadorDto[] ofertas)
        {
            Ofertas = ofertas;
        }
    }
}
