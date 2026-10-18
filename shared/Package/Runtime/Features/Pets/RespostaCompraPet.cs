#nullable enable

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Resposta do servidor sobre a compra de pet no cuidador (PetsMessageIds.RespostaCompraPet).
    /// </summary>
    public sealed class RespostaCompraPet
    {
        public bool Sucesso { get; set; }
        public int Especie { get; set; }
        public string? CodigoErro { get; set; }
        public string? Mensagem { get; set; }
        public long SaldoRestante { get; set; }

        public RespostaCompraPet() { }

        public RespostaCompraPet(
            bool sucesso,
            int especie,
            string? codigoErro = null,
            string? mensagem = null,
            long saldoRestante = 0
        )
        {
            Sucesso = sucesso;
            Especie = especie;
            CodigoErro = codigoErro;
            Mensagem = mensagem;
            SaldoRestante = saldoRestante;
        }
    }
}
