using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    [Serializable]
    public class SaldoCoroasResponse
    {
        public long Saldo;

        public SaldoCoroasResponse() { }

        public SaldoCoroasResponse(long saldo)
        {
            Saldo = saldo;
        }
    }

    [Serializable]
    public class PedidoComprarItem
    {
        public int ItemId;
        public int Quantidade = 1;

        public PedidoComprarItem() { }

        public PedidoComprarItem(int itemId, int quantidade = 1)
        {
            ItemId = itemId;
            Quantidade = quantidade;
        }
    }

    [Serializable]
    public class PedidoVenderItem
    {
        public long InstanciaId;

        public PedidoVenderItem() { }

        public PedidoVenderItem(long instanciaId)
        {
            InstanciaId = instanciaId;
        }
    }

    [Serializable]
    public class PedidoRepararItem
    {
        public long InstanciaId;

        public PedidoRepararItem() { }

        public PedidoRepararItem(long instanciaId)
        {
            InstanciaId = instanciaId;
        }
    }
}
