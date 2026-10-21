namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Representa uma oferta de montaria disponível para compra no estábulo da cidade (#1067).
    /// </summary>
    [System.Serializable]
    public sealed class MontariaEstabuloDto
    {
        public int TipoMontaria;
        public string Nome = "";
        public long PrecoCoroas;
        public string Descricao = "";

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
