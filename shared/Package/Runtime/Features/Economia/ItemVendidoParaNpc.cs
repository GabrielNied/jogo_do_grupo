using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: confirma a venda do item ao NPC e o crédito em coroas.
    /// Id: <see cref="EconomiaMessageIds.ItemVendidoParaNpc"/>.
    /// </summary>
    [Serializable]
    public class ItemVendidoParaNpc
    {
        public long InstanciaId;
        public int ItemId;
        public long CoroasRecebidas;
        public long SaldoAtualizado;

        public ItemVendidoParaNpc() { }

        public ItemVendidoParaNpc(
            long instanciaId,
            int itemId,
            long coroasRecebidas,
            long saldoAtualizado
        )
        {
            InstanciaId = instanciaId;
            ItemId = itemId;
            CoroasRecebidas = coroasRecebidas;
            SaldoAtualizado = saldoAtualizado;
        }
    }
}
