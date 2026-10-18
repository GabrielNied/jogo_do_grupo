namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Servidor → Client: resultado da solicitação de compra no estábulo (#1067).
    /// </summary>
    public sealed class RespostaCompraEstabulo
    {
        public bool Sucesso { get; set; }
        public int TipoMontaria { get; set; }
        public long SaldoRestante { get; set; }
        public string CodigoErro { get; set; }
        public string Mensagem { get; set; }

        public RespostaCompraEstabulo() { }

        public RespostaCompraEstabulo(
            bool sucesso,
            int tipoMontaria,
            long saldoRestante,
            string codigoErro = null,
            string mensagem = null
        )
        {
            Sucesso = sucesso;
            TipoMontaria = tipoMontaria;
            SaldoRestante = saldoRestante;
            CodigoErro = codigoErro;
            Mensagem = mensagem;
        }
    }
}
