using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: envia a lista de itens vendidos na loja do NPC de uma categoria (#1066).
    /// Id: <see cref="EconomiaMessageIds.RespostaCatalogoLoja"/>.
    /// </summary>
    [Serializable]
    public class RespostaCatalogoLoja
    {
        public CategoriaLoja Categoria;
        public int NpcId;
        public ItemLojaDto[] Itens = new ItemLojaDto[0];

        public RespostaCatalogoLoja() { }

        public RespostaCatalogoLoja(
            ItemLojaDto[] itens,
            CategoriaLoja categoria = CategoriaLoja.Nenhuma,
            int npcId = 0
        )
        {
            Itens = itens;
            Categoria = categoria;
            NpcId = npcId;
        }
    }
}
