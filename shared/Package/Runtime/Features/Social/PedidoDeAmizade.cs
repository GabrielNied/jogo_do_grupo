namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Um pedido de amizade ainda sem resposta, em <see cref="ListaDeAmigos.PedidosRecebidos"/>
    /// ou <see cref="ListaDeAmigos.PedidosEnviados"/>. Pedido não vence com o tempo.
    /// </summary>
    [System.Serializable]
    public class PedidoDeAmizade
    {
        /// <summary>Gerado pelo servidor.</summary>
        public long PedidoId = 0;

        /// <summary>
        /// Nome do personagem do outro lado: quem pediu (nos recebidos) ou o alvo (nos enviados).
        /// </summary>
        public string Nome = "";
    }
}
