using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: notifica recusa em operação de economia (saldo insuficiente, etc).
    /// Id: <see cref="EconomiaMessageIds.EconomiaRecusada"/>.
    /// </summary>
    [Serializable]
    public class EconomiaRecusada
    {
        public string Motivo = "";
        public string Mensagem = "";

        public EconomiaRecusada() { }

        public EconomiaRecusada(string motivo, string mensagem = "")
        {
            Motivo = motivo;
            Mensagem = mensagem;
        }
    }
}
