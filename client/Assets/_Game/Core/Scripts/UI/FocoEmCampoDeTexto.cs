using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Detecta quando a pessoa está digitando num <see cref="TextField"/> (UI Toolkit), inclusive
    /// no input interno <c>unity-text-input</c>. Suporta o HUD e raízes ou documentos adicionais.
    /// </summary>
    public static class FocoEmCampoDeTexto
    {
        private static readonly List<VisualElement> _raizesAdicionais = new List<VisualElement>();
        private static readonly List<Func<VisualElement>> _provedoresDeRaiz =
            new List<Func<VisualElement>>();

        /// <summary>
        /// Registra uma raiz de interface adicional para monitoramento de foco.
        /// </summary>
        public static void RegistrarRaiz(VisualElement raiz)
        {
            if (raiz != null && !_raizesAdicionais.Contains(raiz))
            {
                _raizesAdicionais.Add(raiz);
            }
        }

        /// <summary>
        /// Remove uma raiz registrada anteriormente.
        /// </summary>
        public static void DesregistrarRaiz(VisualElement raiz)
        {
            if (raiz != null)
            {
                _raizesAdicionais.Remove(raiz);
            }
        }

        /// <summary>
        /// Registra um provedor de raiz de interface para verificação dinâmica.
        /// </summary>
        public static void RegistrarProvedorDeRaiz(Func<VisualElement> provedor)
        {
            if (provedor != null && !_provedoresDeRaiz.Contains(provedor))
            {
                _provedoresDeRaiz.Add(provedor);
            }
        }

        /// <summary>
        /// Remove um provedor de raiz registrado.
        /// </summary>
        public static void DesregistrarProvedorDeRaiz(Func<VisualElement> provedor)
        {
            if (provedor != null)
            {
                _provedoresDeRaiz.Remove(provedor);
            }
        }

        /// <summary>
        /// Registra um <see cref="UIDocument"/> para monitoramento de foco.
        /// </summary>
        public static void RegistrarDocumento(UIDocument documento)
        {
            if (documento != null)
            {
                RegistrarProvedorDeRaiz(() =>
                    documento != null ? documento.rootVisualElement : null
                );
            }
        }

        /// <summary>
        /// Remove o monitoramento de um <see cref="UIDocument"/>.
        /// </summary>
        public static void DesregistrarDocumento(UIDocument documento)
        {
            if (documento != null && documento.rootVisualElement != null)
            {
                DesregistrarRaiz(documento.rootVisualElement);
            }
        }

        /// <summary>
        /// <c>true</c> se o foco está num campo de texto (ou dentro dele) no HUD ou em raízes registradas.
        /// </summary>
        public static bool TemCampoComFoco(VisualElement raizDoHud = null)
        {
            var raizPrincipal = raizDoHud ?? Navegacao.Container;
            if (TemCampoComFocoNaRaiz(raizPrincipal))
            {
                return true;
            }

            for (var i = _raizesAdicionais.Count - 1; i >= 0; i--)
            {
                var raiz = _raizesAdicionais[i];
                if (raiz == null)
                {
                    _raizesAdicionais.RemoveAt(i);
                    continue;
                }

                if (TemCampoComFocoNaRaiz(raiz))
                {
                    return true;
                }
            }

            for (var i = _provedoresDeRaiz.Count - 1; i >= 0; i--)
            {
                var provedor = _provedoresDeRaiz[i];
                var raiz = provedor?.Invoke();
                if (raiz != null && TemCampoComFocoNaRaiz(raiz))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TemCampoComFocoNaRaiz(VisualElement raiz)
        {
            var focado = raiz?.panel?.focusController?.focusedElement as VisualElement;
            return CampoNaHierarquia(focado) != null;
        }

        /// <summary>Tira o foco do campo; o Esc do menu não abre enquanto a pessoa digita.</summary>
        public static void SoltarFoco(VisualElement raizDoHud = null)
        {
            var raizPrincipal = raizDoHud ?? Navegacao.Container;
            SoltarFocoNaRaiz(raizPrincipal);

            foreach (var raiz in _raizesAdicionais)
            {
                SoltarFocoNaRaiz(raiz);
            }

            foreach (var provedor in _provedoresDeRaiz)
            {
                SoltarFocoNaRaiz(provedor?.Invoke());
            }
        }

        private static void SoltarFocoNaRaiz(VisualElement raiz)
        {
            if (raiz?.panel?.focusController?.focusedElement is VisualElement focado)
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

        /// <summary>Só para testes EditMode.</summary>
        internal static void LimparRegistros()
        {
            _raizesAdicionais.Clear();
            _provedoresDeRaiz.Clear();
        }
    }
}
