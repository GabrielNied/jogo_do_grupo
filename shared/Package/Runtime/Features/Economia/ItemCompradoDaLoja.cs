using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: notifica compra de item concluída com sucesso.
    /// Id: <see cref="EconomiaMessageIds.ItemCompradoDaLoja"/>.
    /// </summary>
    [Serializable]
    public class ItemCompradoDaLoja
    {
        public int ItemId;
        public int Quantidade;
        public long CustoTotal;
        public long SaldoAtualizado;

        public ItemCompradoDaLoja() { }

        public ItemCompradoDaLoja(int itemId, int quantidade, long custoTotal, long saldoAtualizado)
        {
            ItemId = itemId;
            Quantidade = quantidade;
            CustoTotal = custoTotal;
            SaldoAtualizado = saldoAtualizado;
        }
    }
}
