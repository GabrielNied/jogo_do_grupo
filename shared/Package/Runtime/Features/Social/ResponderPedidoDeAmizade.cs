namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client aceita ou recusa um pedido que está em <see cref="ListaDeAmigos.PedidosRecebidos"/>.
    /// Quem pediu não é avisado da recusa.
    /// Id: <see cref="SocialMessageIds.ResponderPedidoDeAmizade"/>.
    /// </summary>
    [System.Serializable]
    public class ResponderPedidoDeAmizade
    {
        /// <summary>O mesmo valor de <see cref="PedidoDeAmizade.PedidoId"/>.</summary>
        public long PedidoId = 0;

        /// <summary>Verdadeiro aceita o pedido; falso recusa.</summary>
        public bool Aceitar = false;
    }
}
