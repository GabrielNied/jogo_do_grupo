using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// A interface de Itens no mundo, em UI Toolkit montada em código: o painel do inventário
    /// (com os ganhos do baú, quando houver) e a dica "E: abrir o baú". Só mostra o que recebe;
    /// quem decide o que aparece é o <see cref="ControleItens"/>.
    /// </summary>
    internal sealed class PainelItens
    {
        private static readonly Color CorFundo = new Color(0f, 0f, 0f, 0.55f);

        private readonly VisualElement _fundo;
        private readonly Label _titulo;
        private readonly Label _aviso;
        private readonly Label _tituloGanhos;
        private readonly VisualElement _ganhos;
        private readonly ScrollView _lista;
        private readonly Label _dica;

        public PainelItens(VisualElement raiz, Action aoFechar)
        {
            _fundo = new VisualElement { name = "itens.painel" };
            _fundo.AddToClassList(EstiloCore.Tela);
            _fundo.style.backgroundColor = CorFundo;

            var painel = new VisualElement();
            painel.AddToClassList(EstiloCore.Painel);
            painel.style.width = 640;
            painel.style.maxHeight = Length.Percent(90);
            painel.style.paddingTop = 24;
            painel.style.paddingBottom = 24;
            painel.style.paddingLeft = 32;
            painel.style.paddingRight = 32;

            _titulo = new Label();
            _titulo.AddToClassList(EstiloCore.Titulo);
            _titulo.style.fontSize = 40;

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);

            _tituloGanhos = new Label("Ganhos desta abertura");
            _tituloGanhos.AddToClassList(EstiloCore.Subtitulo);
            _tituloGanhos.style.fontSize = 24;
            _tituloGanhos.style.marginBottom = 8;

            _ganhos = new VisualElement();
            _ganhos.style.marginBottom = 16;

            var tituloLista = new Label("Itens");
            tituloLista.AddToClassList(EstiloCore.Subtitulo);
            tituloLista.style.fontSize = 24;
            tituloLista.style.marginBottom = 8;

            _lista = new ScrollView(ScrollViewMode.Vertical);
            _lista.style.height = 320;
            _lista.style.flexShrink = 1;

            var fechar = new Button(() => aoFechar?.Invoke()) { text = "Fechar" };
            fechar.AddToClassList(EstiloCore.Botao);
            fechar.style.marginTop = 16;

            painel.Add(_titulo);
            painel.Add(_aviso);
            painel.Add(_tituloGanhos);
            painel.Add(_ganhos);
            painel.Add(tituloLista);
            painel.Add(_lista);
            painel.Add(fechar);
            _fundo.Add(painel);
            raiz.Add(_fundo);

            // A dica não pode pegar o mouse nem esconder o jogo: texto solto embaixo da tela.
            _dica = new Label();
            _dica.pickingMode = PickingMode.Ignore;
            _dica.style.position = Position.Absolute;
            _dica.style.left = 0;
            _dica.style.right = 0;
            _dica.style.bottom = 120;
            _dica.style.fontSize = 28;
            _dica.style.unityTextAlign = TextAnchor.MiddleCenter;
            _dica.style.color = new Color(1f, 1f, 1f, 0.95f);
            raiz.Add(_dica);

            Esconder();
            EsconderDica();
        }

        /// <summary>Se o painel do inventário está na tela.</summary>
        public bool Visivel => _fundo.style.display != DisplayStyle.None;

        /// <summary>Abre o painel com "Carregando" enquanto o servidor responde.</summary>
        public void MostrarCarregando(string titulo)
        {
            _titulo.text = titulo;
            Aviso(null, false);
            DefinirGanhos(null);
            DefinirLista(null);
            _lista.Add(Linha("Carregando..."));
            _fundo.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Atualiza o painel (e o abre). <paramref name="ganhos"/> vazio/nulo esconde a seção de
        /// ganhos; <paramref name="aviso"/> nulo não mostra mensagem.
        /// </summary>
        public void Mostrar(
            string titulo,
            string aviso,
            bool avisoEhErro,
            IReadOnlyList<string> ganhos,
            IReadOnlyList<string> inventario
        )
        {
            _titulo.text = titulo;
            Aviso(aviso, avisoEhErro);
            DefinirGanhos(ganhos);
            DefinirLista(inventario);
            _fundo.style.display = DisplayStyle.Flex;
        }

        public void Esconder()
        {
            _fundo.style.display = DisplayStyle.None;
        }

        public void MostrarDica(string texto)
        {
            _dica.text = texto;
            _dica.style.display = DisplayStyle.Flex;
        }

        public void EsconderDica()
        {
            _dica.style.display = DisplayStyle.None;
        }

        private void Aviso(string texto, bool erro)
        {
            _aviso.text = texto ?? "";
            _aviso.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _aviso.EnableInClassList(EstiloCore.MensagemErro, erro);
            _aviso.EnableInClassList(EstiloCore.MensagemAviso, false);
        }

        private void DefinirGanhos(IReadOnlyList<string> ganhos)
        {
            _ganhos.Clear();
            var temGanhos = ganhos != null && ganhos.Count > 0;
            _tituloGanhos.style.display = temGanhos ? DisplayStyle.Flex : DisplayStyle.None;
            _ganhos.style.display = temGanhos ? DisplayStyle.Flex : DisplayStyle.None;
            if (!temGanhos)
            {
                return;
            }

            foreach (var linha in ganhos)
            {
                var item = Linha("+ " + linha);
                item.style.color = new Color(0.55f, 0.9f, 0.6f);
                _ganhos.Add(item);
            }
        }

        private void DefinirLista(IReadOnlyList<string> inventario)
        {
            _lista.Clear();
            if (inventario == null)
            {
                return;
            }

            if (inventario.Count == 0)
            {
                _lista.Add(Linha("Inventário vazio."));
                return;
            }

            foreach (var linha in inventario)
            {
                _lista.Add(Linha(linha));
            }
        }

        private static Label Linha(string texto)
        {
            var linha = new Label(texto);
            linha.AddToClassList(EstiloCore.Texto);
            linha.style.fontSize = 22;
            linha.style.marginBottom = 2;
            return linha;
        }
    }
}
