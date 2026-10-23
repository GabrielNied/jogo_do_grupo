using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// Folha de estilo das janelas de Economia sem folha própria (vendedor, ferreiro, correio e
    /// banco da guilda): <c>Resources/Economia/EstiloEconomia.uss</c>, só com tokens do Lumora.
    /// </summary>
    internal static class EstiloEconomia
    {
        private const string Caminho = "Economia/EstiloEconomia";

        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo != null && !raiz.styleSheets.Contains(estilo))
                raiz.styleSheets.Add(estilo);
        }
    }
}
