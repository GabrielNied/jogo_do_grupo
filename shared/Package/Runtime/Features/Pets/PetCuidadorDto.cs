namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Informações de um pet à venda no cuidador de pets (#1068).</summary>
    [System.Serializable]
    public sealed class PetCuidadorDto
    {
        public int Especie;
        public string Nome = string.Empty;
        public long PrecoCoroas;
        public string Descricao = string.Empty;
        public string Origem = string.Empty;

        public PetCuidadorDto() { }

        public PetCuidadorDto(
            int especie,
            string nome,
            long precoCoroas,
            string descricao,
            string origem
        )
        {
            Especie = especie;
            Nome = nome;
            PrecoCoroas = precoCoroas;
            Descricao = descricao;
            Origem = origem;
        }
    }
}
