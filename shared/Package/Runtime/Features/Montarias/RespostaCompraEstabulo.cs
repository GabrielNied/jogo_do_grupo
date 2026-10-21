#nullable enable annotations
namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Servidor → Client: resultado da solicitação de compra no estábulo (#1067).
    /// </summary>
    [System.Serializable]
    public sealed class RespostaCompraEstabulo
    {
        public bool Sucesso;
        public int TipoMontaria;
        public long SaldoRestante;
        public string? CodigoErro;
        public string? Mensagem;

        public RespostaCompraEstabulo() { }

        public RespostaCompraEstabulo(
            bool sucesso,
            int tipoMontaria,
            long saldoRestante,
            string? codigoErro = null,
            string? mensagem = null
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
