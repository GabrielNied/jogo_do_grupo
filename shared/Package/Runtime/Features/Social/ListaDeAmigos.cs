namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor envia a lista completa de amigos e de pedidos ao entrar no jogo e a cada mudança
    /// (nunca só a diferença). Entrar e sair de um amigo não reenvia a lista: chega em
    /// <see cref="AmigoMudouDeEstado"/>. Id: <see cref="SocialMessageIds.ListaDeAmigos"/>.
    /// </summary>
    [System.Serializable]
    public class ListaDeAmigos
    {
        /// <summary>Amigos em ordem de nome. Nunca nulo.</summary>
        public AmigoDaLista[] Amigos = System.Array.Empty<AmigoDaLista>();

        /// <summary>Pedidos que esperam a resposta deste jogador, em ordem de chegada.</summary>
        public PedidoDeAmizade[] PedidosRecebidos = System.Array.Empty<PedidoDeAmizade>();

        /// <summary>Pedidos que este jogador fez e ainda não tiveram resposta.</summary>
        public PedidoDeAmizade[] PedidosEnviados = System.Array.Empty<PedidoDeAmizade>();
    }
}
