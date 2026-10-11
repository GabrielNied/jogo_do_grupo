using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Barra no canto inferior direito com a tecla de cada janela e a do menu, para a pessoa
    /// descobrir os atalhos sem ler manual. Só mostra: não recebe clique (o cursor fica preso
    /// à câmera durante o jogo). A janela aberta fica destacada.
    /// </summary>
    internal sealed class BarraDeAtalhos
    {
        internal const string NomeElemento = "core.barra-atalhos";
        internal const string TeclaMenu = "Esc";
        internal const string TituloMenu = "Menu";

        private readonly Dictionary<string, VisualElement> _itens =
            new Dictionary<string, VisualElement>();

        private readonly VisualElement _raiz;

        public BarraDeAtalhos(VisualElement container, IReadOnlyList<DefinicaoDeJanela> definicoes)
        {
            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.BarraAtalhos);
            _raiz.pickingMode = PickingMode.Ignore;

            if (definicoes != null)
            {
                foreach (var definicao in definicoes)
                {
                    var item = CriarItem(definicao.Tecla, definicao.Titulo);
                    _itens[definicao.Chave] = item;
                    _raiz.Add(item);
                }
            }

            _raiz.Add(CriarItem(TeclaMenu, TituloMenu));
            container?.Add(_raiz);
        }

        /// <summary>Elemento da barra.</summary>
        public VisualElement Raiz => _raiz;

        /// <summary>Destaca a janela aberta (<c>null</c> tira o destaque de todas).</summary>
        public void Destacar(string chaveAberta)
        {
            foreach (var par in _itens)
            {
                par.Value.EnableInClassList(EstiloCore.AtalhoAtivo, par.Key == chaveAberta);
            }
        }

        /// <summary>Mantém a barra na frente da janela aberta, para o destaque aparecer.</summary>
        public void TrazerParaFrente()
        {
            _raiz.BringToFront();
        }

        public void Remover()
        {
            _raiz.RemoveFromHierarchy();
        }

        private static VisualElement CriarItem(string tecla, string titulo)
        {
            var item = new VisualElement();
            item.AddToClassList(EstiloCore.Atalho);
            item.pickingMode = PickingMode.Ignore;

            var rotuloTecla = new Label(tecla);
            rotuloTecla.AddToClassList(EstiloCore.AtalhoTecla);
            rotuloTecla.pickingMode = PickingMode.Ignore;

            var rotuloTitulo = new Label(titulo);
            rotuloTitulo.AddToClassList(EstiloCore.AtalhoNome);
            rotuloTitulo.pickingMode = PickingMode.Ignore;

            item.Add(rotuloTecla);
            item.Add(rotuloTitulo);
            return item;
        }
    }
}
