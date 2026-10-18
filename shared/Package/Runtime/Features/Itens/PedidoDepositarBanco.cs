using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Pedido para mover um item da bolsa para o banco pessoal da conta.
    /// Se <see cref="TemDestino"/> for falso, o servidor procura a primeira casa livre.
    /// </summary>
    [Serializable]
    public class PedidoDepositarBanco
    {
        public long InstanciaId;
        public bool TemDestino;
        public int Aba;
        public int Casa;
    }
}
