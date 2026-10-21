#nullable enable

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Resposta do servidor sobre a compra de pet no cuidador (PetsMessageIds.RespostaCompraPet).
    /// </summary>
    [System.Serializable]
    public sealed class RespostaCompraPet
    {
        public bool Sucesso;
        public int Especie;
        public string? CodigoErro;
        public string? Mensagem;
        public long SaldoRestante;

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
