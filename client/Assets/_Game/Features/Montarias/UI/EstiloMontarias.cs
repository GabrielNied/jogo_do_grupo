using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// Folha de estilo das janelas de Montarias (<c>Resources/Montarias/EstiloMontarias.uss</c>):
    /// cartão, faixa, selo e oferta do estábulo, só com tokens do Lumora.
    /// </summary>
    internal static class EstiloMontarias
    {
        private const string Caminho = "Montarias/EstiloMontarias";

        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo != null && !raiz.styleSheets.Contains(estilo))
                raiz.styleSheets.Add(estilo);
        }
    }
}
