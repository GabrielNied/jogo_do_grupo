using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Item à venda no catálogo da loja de NPC.
    /// </summary>
    [Serializable]
    public class ItemLojaDto
    {
        public int ItemId;
        public string Nome = "";
        public long PrecoCoroas;

        public ItemLojaDto() { }

        public ItemLojaDto(int itemId, string nome, long precoCoroas)
        {
            ItemId = itemId;
            Nome = nome;
            PrecoCoroas = precoCoroas;
        }
    }
}
