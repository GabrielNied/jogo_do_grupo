using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Pedido para mover um item do banco pessoal da conta para a bolsa do personagem ativo.
    /// Se <see cref="TemDestino"/> for falso, o servidor procura a primeira casa livre na bolsa.
    /// </summary>
    [Serializable]
    public class PedidoRetirarBanco
    {
        public long InstanciaId;
        public bool TemDestino;
        public int Aba;
        public int Casa;
    }
}
