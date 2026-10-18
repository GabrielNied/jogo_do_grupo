namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Informações de um pet à venda no cuidador de pets (#1068).</summary>
    public sealed class PetCuidadorDto
    {
        public int Especie { get; set; }
        public string Nome { get; set; } = string.Empty;
        public long PrecoCoroas { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;

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
