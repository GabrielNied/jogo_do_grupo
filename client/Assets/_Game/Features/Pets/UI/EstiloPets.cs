using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pets.UI
{
    /// <summary>
    /// Folha de estilo das janelas de Pets (<c>Resources/Pets/EstiloPets.uss</c>): cartão, selo,
    /// abas e linhas do cuidador, só com tokens do Lumora.
    /// </summary>
    internal static class EstiloPets
    {
        private const string Caminho = "Pets/EstiloPets";

        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo != null && !raiz.styleSheets.Contains(estilo))
                raiz.styleSheets.Add(estilo);
        }
    }
}
