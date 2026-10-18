using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: envia a lista de itens vendidos na loja do NPC.
    /// Id: <see cref="EconomiaMessageIds.RespostaCatalogoLoja"/>.
    /// </summary>
    [Serializable]
    public class RespostaCatalogoLoja
    {
        public ItemLojaDto[] Itens;

        public RespostaCatalogoLoja() { }

        public RespostaCatalogoLoja(ItemLojaDto[] itens)
        {
            Itens = itens;
        }
    }
}
