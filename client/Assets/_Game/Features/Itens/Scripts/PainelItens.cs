using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O conteúdo da janela do inventário, em UI Toolkit montado em código: o aviso, os ganhos
    /// do baú (quando houver) e a lista de itens. Só mostra o que recebe; quem decide o que
    /// aparece é a <see cref="JanelaInventario"/>. A moldura, o título e o botão de fechar são
    /// do Core (<c>JanelasDoJogo</c>).
    /// </summary>
    internal sealed class PainelItens
    {
        private readonly Action<string> _definirTitulo;
        private readonly Label _aviso;
        private readonly Label _tituloGanhos;
        private readonly VisualElement _ganhos;
        private readonly ScrollView _lista;

        /// <param name="conteudo">Área da janela que recebe os elementos.</param>
        /// <param name="definirTitulo">Troca o título da janela (ex.: "Inventário (3/20)").</param>
        public PainelItens(VisualElement conteudo, Action<string> definirTitulo)
        {
            _definirTitulo = definirTitulo;

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);

            _tituloGanhos = new Label("Ganhos desta abertura");
            _tituloGanhos.AddToClassList(EstiloCore.Subtitulo);

            _ganhos = new VisualElement();

            var tituloLista = new Label("Itens");
            tituloLista.AddToClassList(EstiloCore.Subtitulo);

            _lista = new ScrollView(ScrollViewMode.Vertical);
            _lista.AddToClassList(EstiloCore.Lista);

            conteudo.Add(_aviso);
            conteudo.Add(_tituloGanhos);
            conteudo.Add(_ganhos);
            conteudo.Add(tituloLista);
            conteudo.Add(_lista);

            Aviso(null, false);
            DefinirGanhos(null);
        }

        /// <summary>Mostra "Carregando" enquanto o servidor responde.</summary>
        public void MostrarCarregando(string titulo)
        {
            _definirTitulo?.Invoke(titulo);
            Aviso(null, false);
            DefinirGanhos(null);
            DefinirLista(null);
            _lista.Add(Linha("Carregando..."));
        }

        /// <summary>
        /// Atualiza o conteúdo. <paramref name="ganhos"/> vazio/nulo esconde a seção de ganhos;
        /// <paramref name="aviso"/> nulo não mostra mensagem.
        /// </summary>
        public void Mostrar(
            string titulo,
            string aviso,
            bool avisoEhErro,
            IReadOnlyList<string> ganhos,
            IReadOnlyList<string> inventario
        )
        {
            _definirTitulo?.Invoke(titulo);
            Aviso(aviso, avisoEhErro);
            DefinirGanhos(ganhos);
            DefinirLista(inventario);
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
