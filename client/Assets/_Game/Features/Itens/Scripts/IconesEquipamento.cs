using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Ícones de silhueta branca por tipo de equipamento, em
    /// <c>Resources/Itens/Icones/&lt;TipoEquipamento&gt;</c>. A UI tinge pela raridade.
    /// </summary>
    public static class IconesEquipamento
    {
        public const string PastaResources = "Itens/Icones/";

        /// <summary>Sprite do tipo, ou null se não existir.</summary>
        public static Sprite Carregar(TipoEquipamento tipo)
        {
            return Resources.Load<Sprite>(PastaResources + tipo);
        }
    }
}
