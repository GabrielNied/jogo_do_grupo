using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede compra de item vendido pelo NPC.
    /// Id: <see cref="EconomiaMessageIds.ComprarItemDaLoja"/>.
    /// </summary>
    [Serializable]
    public class ComprarItemDaLoja
    {
        public int ItemId;
        public int Quantidade = 1;

        public ComprarItemDaLoja() { }

        public ComprarItemDaLoja(int itemId, int quantidade = 1)
        {
            ItemId = itemId;
            Quantidade = quantidade;
        }
    }
}
