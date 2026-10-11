using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Detecta quando a pessoa está digitando num <see cref="TextField"/> (UI Toolkit), inclusive
    /// no input interno <c>unity-text-input</c>.
    /// </summary>
    internal static class FocoEmCampoDeTexto
    {
        /// <summary>
        /// <c>true</c> se o foco do painel está num campo de texto (ou dentro dele).
        /// </summary>
        public static bool TemCampoComFoco(VisualElement raizDoHud)
        {
            var focado = raizDoHud?.panel?.focusController?.focusedElement as VisualElement;
            return CampoNaHierarquia(focado) != null;
        }

        /// <summary>Tira o foco do campo; o Esc do menu não abre enquanto a pessoa digita.</summary>
        public static void SoltarFoco(VisualElement raizDoHud)
        {
            if (raizDoHud?.panel?.focusController?.focusedElement is VisualElement focado)
            {
                focado.Blur();
            }
        }

        /// <summary>Sobe a árvore até achar um <see cref="TextField"/>.</summary>
        internal static TextField CampoNaHierarquia(VisualElement elemento)
        {
            for (var atual = elemento; atual != null; atual = atual.parent)
            {
                if (atual is TextField campo)
                {
                    return campo;
                }
            }

            return null;
        }
    }
}
