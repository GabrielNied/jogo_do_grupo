using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
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
            _fundo.AddToClassList(EstiloCore.TelaSobreJogo);

            var painel = new VisualElement();
            painel.AddToClassList(EstiloCore.Painel);
            painel.AddToClassList(EstiloCore.PainelLargo);

            _titulo = new Label();
            _titulo.AddToClassList(EstiloCore.Titulo);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);

            _tituloGanhos = new Label("Ganhos desta abertura");
            _tituloGanhos.AddToClassList(EstiloCore.Subtitulo);

            _ganhos = new VisualElement();

            var tituloLista = new Label("Itens");
            tituloLista.AddToClassList(EstiloCore.Subtitulo);

            _lista = new ScrollView(ScrollViewMode.Vertical);
            _lista.AddToClassList(EstiloCore.Lista);

            var fechar = new Button(() => aoFechar?.Invoke()) { text = "Fechar" };
            fechar.AddToClassList(EstiloCore.Botao);

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
            _dica.AddToClassList(EstiloCore.Dica);
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
                item.AddToClassList(EstiloCore.TextoGanho);
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
            linha.AddToClassList(EstiloCore.LinhaLista);
            return linha;
        }
    }
}
