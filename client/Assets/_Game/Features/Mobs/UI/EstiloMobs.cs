using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Aplica a folha <c>Resources/UI/EstiloMobs.uss</c> nas janelas de Mobs (bestiário),
    /// com as classes <c>bestiario-*</c>, só com tokens do Lumora.
    /// </summary>
    internal static class EstiloMobs
    {
        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(PainelDeInimigos.CaminhoEstilo);
            if (estilo != null && !raiz.styleSheets.Contains(estilo))
                raiz.styleSheets.Add(estilo);
        }
    }
}
