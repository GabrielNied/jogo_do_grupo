using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede compra de item vendido pelo NPC (#1066).
    /// Id: <see cref="EconomiaMessageIds.ComprarItemDaLoja"/>.
    /// </summary>
    [Serializable]
    public class ComprarItemDaLoja
    {
        public int ItemId;
        public int Quantidade = 1;
        public CategoriaLoja Categoria;
        public int NpcId;

        public ComprarItemDaLoja() { }

        public ComprarItemDaLoja(
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
}
