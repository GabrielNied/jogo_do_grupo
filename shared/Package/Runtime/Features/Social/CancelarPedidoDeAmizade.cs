namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client desiste de um pedido que está em <see cref="ListaDeAmigos.PedidosEnviados"/>.
    /// Id: <see cref="SocialMessageIds.CancelarPedidoDeAmizade"/>.
    /// </summary>
    [System.Serializable]
    public class CancelarPedidoDeAmizade
    {
        /// <summary>O mesmo valor de <see cref="PedidoDeAmizade.PedidoId"/>.</summary>
        public long PedidoId = 0;
    }
}
