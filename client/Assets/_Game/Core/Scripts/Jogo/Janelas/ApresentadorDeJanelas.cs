using System;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Põe a janela aberta na tela: a mesma moldura para todas (véu sobre o jogo, título,
    /// botão de fechar, conteúdo da funcionalidade e a dica de como fechar).
    /// </summary>
    internal sealed class ApresentadorDeJanelas
    {
        internal const string NomeElemento = "core.janela";
        internal const string TextoFechar = "X";
        internal const string TextoFalha = "Não foi possível abrir esta janela.";

        private readonly VisualElement _container;
        private readonly Action _aoPedirFechar;

        private VisualElement _raiz;
        private JanelaDoJogo _janela;

        /// <param name="container">Elemento que recebe a janela (o das telas do Core).</param>
        /// <param name="aoPedirFechar">Chamado quando a pessoa clica no botão de fechar.</param>
        public ApresentadorDeJanelas(VisualElement container, Action aoPedirFechar)
        {
            _container = container;
            _aoPedirFechar = aoPedirFechar;
        }

        /// <summary>Elemento da janela na tela, ou <c>null</c> com tudo fechado.</summary>
        public VisualElement Raiz => _raiz;

        /// <summary>Texto da dica do rodapé (ex.: "I ou Esc: fechar").</summary>
        internal static string TextoDoRodape(string tecla)
        {
            return $"{tecla} ou Esc: fechar";
        }

        public void Mostrar(DefinicaoDeJanela definicao)
        {
            Esconder();
            if (_container == null || definicao == null)
            {
                return;
            }

            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.Tela);
            _raiz.AddToClassList(EstiloCore.TelaSobreJogo);

            var painel = EstiloCore.CriarPainel();
            painel.AddToClassList(EstiloCore.PainelLargo);
            painel.AddToClassList(EstiloCore.Janela);

            var titulo = new Label(definicao.Titulo);
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList(EstiloCore.JanelaTitulo);

            var fechar = new Button(() => _aoPedirFechar?.Invoke()) { text = TextoFechar };
            fechar.AddToClassList(EstiloCore.Botao);
            fechar.AddToClassList(EstiloCore.JanelaFechar);
            fechar.tooltip = "Fechar";

            var cabecalho = new VisualElement();
            cabecalho.AddToClassList(EstiloCore.JanelaCabecalho);
            cabecalho.Add(titulo);
            cabecalho.Add(fechar);

            var conteudo = new VisualElement { name = definicao.Chave };
            conteudo.AddToClassList(EstiloCore.JanelaConteudo);

            var rodape = new Label(
                TextoDoRodape(TeclasDoJogo.Rotulo(TeclasDoJogo.Janela(definicao.Chave)))
            );
            rodape.AddToClassList(EstiloCore.JanelaRodape);

            painel.Add(cabecalho);
            painel.Add(conteudo);
            painel.Add(rodape);
            _raiz.Add(painel);
            _container.Add(_raiz);

            // Janela com defeito não pode travar o jogo: mostra o aviso e segue fechável.
            JanelaDoJogo janela = null;
            try
            {
                janela = definicao.Criar();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (janela == null)
            {
                Debug.LogError($"[JanelasDoJogo] A janela \"{definicao.Chave}\" não foi criada.");
                MostrarFalha(conteudo);
                return;
            }

            _janela = janela;
            painel.EnableInClassList("ui-painel-social", janela.MolduraSocial);
            try
            {
                janela.Abrir(conteudo, texto => titulo.text = texto);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                conteudo.Clear();
                MostrarFalha(conteudo);
            }
        }

        public void Esconder()
        {
            var janela = _janela;
            _janela = null;
            if (janela != null)
            {
                try
                {
                    janela.Fechar();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _raiz?.RemoveFromHierarchy();
            _raiz = null;
        }

        private static void MostrarFalha(VisualElement conteudo)
        {
            var falha = new Label(TextoFalha);
            falha.AddToClassList(EstiloCore.Mensagem);
            falha.AddToClassList(EstiloCore.MensagemErro);
            conteudo.Add(falha);
        }
    }
}
