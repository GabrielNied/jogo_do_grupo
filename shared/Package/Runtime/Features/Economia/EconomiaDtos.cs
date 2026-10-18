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
        public CategoriaLoja Categoria;
        public int NpcId;

        public PedidoComprarItem() { }

        public PedidoComprarItem(
            int itemId,
            int quantidade = 1,
            CategoriaLoja categoria = CategoriaLoja.Nenhuma,
            int npcId = 0
        )
        {
            ItemId = itemId;
            Quantidade = quantidade;
            Categoria = categoria;
            NpcId = npcId;
        }
    }

    [Serializable]
    public class PedidoVenderItem
    {
        public long InstanciaId;
        public int NpcId;

        public PedidoVenderItem() { }

        public PedidoVenderItem(long instanciaId, int npcId = 0)
        {
            InstanciaId = instanciaId;
            NpcId = npcId;
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
