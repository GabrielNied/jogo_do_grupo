using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Pedido para mover um item entre casas do próprio banco pessoal da conta.
    /// Se a casa de destino estiver ocupada, os dois itens trocam de posição.
    /// </summary>
    [Serializable]
    public class PedidoMoverBanco
    {
        public long InstanciaId;
        public int Aba;
        public int Casa;
    }
}
