using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Menu rápido no canto inferior direito, como a "micro barra" do World of Warcraft: um
    /// ícone por janela com tecla (e o do menu), a tecla no canto e o nome na dica. Com o
    /// cursor livre (alguma janela aberta) o clique abre e fecha a janela; com o cursor preso
    /// ele só mostra. As janelas abertas ficam acesas.
    /// </summary>
    internal sealed class BarraDeAtalhos
    {
        internal const string NomeElemento = "core.barra-atalhos";
        internal const string TeclaMenu = "Esc";
        internal const string TituloMenu = "Menu";

        // Ícone de cada janela conhecida (Resources/Interface/Icones). Janela nova sem ícone
        // mostra a inicial do título.
        private static readonly Dictionary<string, string> IconePorChave = new Dictionary<
            string,
            string
        >
        {
            ["personagem.ficha"] = "personagem",
            ["itens.inventario"] = "inventario",
            ["itens.banco"] = "banco",
            ["missoes.registro"] = "missoes",
            ["skills.livro"] = "skills",
            ["mundo.mapa"] = "mapa",
            ["montarias.colecao"] = "montarias",
            ["social.grupo"] = "social",
            ["social.guilda"] = "guilda",
            ["conquistas.registro"] = "conquistas",
            ["pets.companheiros"] = "companheiros",
            ["talentos.arvore"] = "talentos",
            ["mobs.bestiario"] = "bestiario",
            ["desenvolvimento.reporte"] = "reporte",
            ["profissoes.forja"] = "profissoes",
        };

        private readonly Dictionary<string, VisualElement> _itens =
            new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> _teclas = new Dictionary<string, Label>();

        private readonly VisualElement _raiz;
        private readonly Action<string> _aoClicar;
        private readonly Action _aoClicarMenu;

        public BarraDeAtalhos(
            VisualElement container,
            IReadOnlyList<DefinicaoDeJanela> definicoes,
            Action<string> aoClicar = null,
            Action aoClicarMenu = null
        )
        {
            _aoClicar = aoClicar;
            _aoClicarMenu = aoClicarMenu;
            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.BarraAtalhos);
            _raiz.pickingMode = PickingMode.Ignore;

            var lista = new List<DefinicaoDeJanela>();
            if (definicoes != null)
            {
                foreach (var definicao in definicoes)
                {
                    if (RegistroDeJanelas.TeclaValida(definicao.Tecla))
                        lista.Add(definicao);
                }
            }

            lista.Sort((a, b) => a.Ordem.CompareTo(b.Ordem));
            foreach (var definicao in lista)
            {
                var chave = definicao.Chave;
                var item = CriarItem(
                    chave,
                    TeclasDoJogo.Rotulo(TeclasDoJogo.Janela(chave)),
                    definicao.Titulo,
                    IconePorChave.TryGetValue(chave, out var icone) ? icone : null,
                    () => _aoClicar?.Invoke(chave)
                );
                _itens[chave] = item;
                _raiz.Add(item);
            }

            var menu = CriarItem(
                null,
                TeclaMenu,
                TituloMenu,
                "menu",
                () => _aoClicarMenu?.Invoke()
            );
            menu.AddToClassList("atalho-menu");
            _raiz.Add(menu);
            container?.Add(_raiz);
            TeclasDoJogo.Mudou += AtualizarTeclas;
        }

        private void AtualizarTeclas()
        {
            foreach (var par in _teclas)
                par.Value.text = TeclasDoJogo.Rotulo(TeclasDoJogo.Janela(par.Key));
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

        /// <summary>Destaca todas as janelas abertas (lista vazia tira o destaque de todas).</summary>
        public void DestacarAbertas(IReadOnlyList<string> chavesAbertas)
        {
            foreach (var par in _itens)
            {
                var aberta = false;
                for (var i = 0; i < chavesAbertas.Count && !aberta; i++)
                {
                    aberta = chavesAbertas[i] == par.Key;
                }

                par.Value.EnableInClassList(EstiloCore.AtalhoAtivo, aberta);
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

        public void Destruir()
        {
            TeclasDoJogo.Mudou -= AtualizarTeclas;
            Remover();
        }

        private VisualElement CriarItem(
            string chave,
            string tecla,
            string titulo,
            string icone,
            Action aoClicar
        )
        {
            var item = new VisualElement();
            item.AddToClassList(EstiloCore.Atalho);
            item.pickingMode = PickingMode.Position;
            item.RegisterCallback<ClickEvent>(_ => aoClicar?.Invoke());

            var textura = ComponentesHud.Icone("Icones", icone);
            if (textura != null)
            {
                item.Add(ComponentesHud.Imagem(textura, "atalho-icone"));
            }
            else
            {
                var inicial = ComponentesHud.Texto(
                    string.IsNullOrEmpty(titulo) ? "?" : titulo.Substring(0, 1),
                    "atalho-inicial"
                );
                item.Add(inicial);
            }

            var rotuloTecla = new Label(tecla);
            rotuloTecla.AddToClassList(EstiloCore.AtalhoTecla);
            rotuloTecla.pickingMode = PickingMode.Ignore;
            item.Add(rotuloTecla);
            if (chave != null)
                _teclas[chave] = rotuloTecla;

            // O nome fica na dica (e escondido no layout), para a barra caber em uma linha.
            var rotuloTitulo = new Label(titulo);
            rotuloTitulo.AddToClassList(EstiloCore.AtalhoNome);
            rotuloTitulo.pickingMode = PickingMode.Ignore;
            item.Add(rotuloTitulo);

            ComponentesHud.ComDica(
                item,
                () => new ConteudoDaDica { Titulo = titulo, Rodape = "Atalho: " + rotuloTecla.text }
            );
            return item;
        }
    }
}
