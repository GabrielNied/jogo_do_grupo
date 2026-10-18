using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede o catálogo de itens à venda pelo NPC de uma categoria ou ID (#1066).
    /// Id: <see cref="EconomiaMessageIds.ConsultarCatalogoLoja"/>.
    /// </summary>
    [Serializable]
    public class ConsultarCatalogoLoja
    {
        public CategoriaLoja Categoria;
        public int NpcId;

        public ConsultarCatalogoLoja() { }

        public ConsultarCatalogoLoja(CategoriaLoja categoria, int npcId = 0)
        {
            Categoria = categoria;
            NpcId = npcId;
        }
    }
}
