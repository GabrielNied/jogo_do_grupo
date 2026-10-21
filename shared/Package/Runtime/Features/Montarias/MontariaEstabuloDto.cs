namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Representa uma oferta de montaria disponível para compra no estábulo da cidade (#1067).
    /// </summary>
    public sealed class MontariaEstabuloDto
    {
        public int TipoMontaria { get; set; }
        public string Nome { get; set; } = "";
        public long PrecoCoroas { get; set; }
        public string Descricao { get; set; } = "";

        public MontariaEstabuloDto() { }

        public MontariaEstabuloDto(
            int tipoMontaria,
            string nome,
            long precoCoroas,
            string descricao
        )
        {
            TipoMontaria = tipoMontaria;
            Nome = nome;
            PrecoCoroas = precoCoroas;
            Descricao = descricao;
        }
    }
}
