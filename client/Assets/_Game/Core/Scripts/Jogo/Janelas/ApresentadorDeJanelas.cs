using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Põe as janelas abertas na tela, várias ao mesmo tempo, com a mesma moldura para todas
    /// (título, botão de fechar, conteúdo da funcionalidade e a dica de como fechar). As
    /// comuns ficam lado a lado numa área sem véu, para o jogo seguir à vista; a de tela
    /// inteira (o mapa) fica por baixo delas.
    /// </summary>
    internal sealed class ApresentadorDeJanelas
    {
        internal const string NomeElemento = "core.janela";
        internal const string NomeArea = "core.janelas";
        internal const string TextoFechar = "X";
        internal const string TextoFalha = "Não foi possível abrir esta janela.";

        private readonly VisualElement _container;
        private readonly Action<string> _aoPedirFechar;
        private readonly Action<string> _aoFocar;
        private readonly VisualElement _area;

        // Na ordem em que foram mostradas; a focada é a de cima.
        private readonly List<Aberta> _abertas = new List<Aberta>();
        private Aberta _focada;

        /// <param name="container">Elemento que recebe as janelas (o das telas do Core).</param>
        /// <param name="aoPedirFechar">Chamado com a chave quando a pessoa clica no X.</param>
        /// <param name="aoFocar">Chamado com a chave quando a pessoa clica numa janela.</param>
        public ApresentadorDeJanelas(
            VisualElement container,
            Action<string> aoPedirFechar,
            Action<string> aoFocar = null
        )
        {
            _container = container;
            _aoPedirFechar = aoPedirFechar;
            _aoFocar = aoFocar;
            _area = new VisualElement { name = NomeArea, pickingMode = PickingMode.Ignore };
            _area.AddToClassList(EstiloCore.JanelasArea);
        }

        /// <summary>Elemento da janela focada (a de cima), ou <c>null</c> com tudo fechado.</summary>
        public VisualElement Raiz => _focada?.Raiz;

        /// <summary>Área onde ficam as janelas comuns, lado a lado.</summary>
        public VisualElement Area => _area;

        public bool TelaInteiraAberta
        {
            get
            {
                foreach (var aberta in _abertas)
                {
                    if (aberta.Janela != null && aberta.Janela.TelaInteira)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Texto da dica do rodapé (ex.: "I ou Esc: fechar").</summary>
        internal static string TextoDoRodape(string tecla)
        {
            return string.IsNullOrEmpty(tecla) ? "Esc: fechar" : $"{tecla} ou Esc: fechar";
        }

        /// <summary>
        /// Se o ponto da tela (em pixels, origem embaixo à esquerda, como o mouse) está sobre
        /// alguma janela aberta.
        /// </summary>
        public bool ContemPontoDaTela(Vector2 ponto)
        {
            var painel = _container?.panel;
            if (painel == null || _abertas.Count == 0)
            {
                return false;
            }

            var noPainel = RuntimePanelUtils.ScreenToPanel(
                painel,
                new Vector2(ponto.x, Screen.height - ponto.y)
            );
            foreach (var aberta in _abertas)
            {
                if (aberta.Painel.worldBound.Contains(noPainel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Se o ponto da tela (em pixels, origem embaixo à esquerda) está sobre a interface: uma
        /// janela aberta ou qualquer elemento que recebe clique (chat, convite, botões). Fora
        /// disso o clique é no mundo.
        /// </summary>
        public bool SobreInterface(Vector2 ponto)
        {
            if (ContemPontoDaTela(ponto))
            {
                return true;
            }

            var painel = _container?.panel;
            if (painel == null)
            {
                return false;
            }

            var noPainel = RuntimePanelUtils.ScreenToPanel(
                painel,
                new Vector2(ponto.x, Screen.height - ponto.y)
            );
            var tocado = painel.Pick(noPainel);
            return tocado != null && tocado != _container && tocado != painel.visualTree;
        }

        public void Mostrar(DefinicaoDeJanela definicao)
        {
            if (_container == null || definicao == null)
            {
                return;
            }

            Esconder(definicao);

            JanelaDoJogo janela = null;
            try
            {
                janela = definicao.Criar();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var telaInteira = janela != null && janela.TelaInteira;
            var chave = definicao.Chave;

            var raiz = new VisualElement { name = NomeElemento };
            if (telaInteira)
            {
                raiz.AddToClassList(EstiloCore.Tela);
                raiz.AddToClassList(EstiloCore.TelaSobreJogo);
                raiz.AddToClassList("janela-raiz-tela-inteira");
            }
            else
            {
                raiz.AddToClassList(EstiloCore.JanelaRaiz);
            }

            VisualElement painel = telaInteira ? new VisualElement() : EstiloCore.CriarPainel();
            if (!telaInteira)
            {
                painel.AddToClassList(EstiloCore.PainelLargo);
            }
            painel.AddToClassList(EstiloCore.Janela);
            painel.EnableInClassList("janela-tela-inteira", telaInteira);
            // Clicar em qualquer parte da janela a traz para cima (o Esc fecha ela primeiro).
            painel.RegisterCallback<PointerDownEvent>(
                _ => _aoFocar?.Invoke(chave),
                TrickleDown.TrickleDown
            );

            var titulo = new Label(definicao.Titulo);
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList(EstiloCore.JanelaTitulo);

            var fechar = new Button(() => _aoPedirFechar?.Invoke(chave)) { text = TextoFechar };
            fechar.AddToClassList(EstiloCore.Botao);
            fechar.AddToClassList(EstiloCore.JanelaFechar);
            fechar.tooltip = "Fechar";

            var cabecalho = new VisualElement();
            cabecalho.AddToClassList(EstiloCore.JanelaCabecalho);
            cabecalho.Add(titulo);
            cabecalho.Add(fechar);

            var conteudo = new VisualElement { name = chave };
            conteudo.AddToClassList(EstiloCore.JanelaConteudo);

            var rodape = new Label(TextoDoRodape(TeclasDoJogo.Rotulo(TeclasDoJogo.Janela(chave))));
            rodape.AddToClassList(EstiloCore.JanelaRodape);

            if (telaInteira)
            {
                painel.Add(conteudo);
                painel.Add(cabecalho);
                painel.Add(rodape);
            }
            else
            {
                painel.Add(cabecalho);
                painel.Add(conteudo);
                painel.Add(rodape);
            }
            raiz.Add(painel);

            if (telaInteira)
            {
                // Por baixo das janelas comuns, que continuam à vista sobre o mapa.
                _container.Add(raiz);
                if (_area.parent == _container)
                {
                    _area.BringToFront();
                }
            }
            else
            {
                if (_area.parent != _container)
                {
                    _container.Add(_area);
                }
                _area.Add(raiz);
            }

            var aberta = new Aberta(chave, raiz, painel);
            _abertas.Add(aberta);
            Destacar(aberta);

            // Janela com defeito não pode travar o jogo: mostra o aviso e segue fechável.
            if (janela == null)
            {
                Debug.LogError($"[JanelasDoJogo] A janela \"{chave}\" não foi criada.");
                MostrarFalha(conteudo);
                return;
            }

            aberta.Janela = janela;
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

        /// <summary>Marca a janela como a de cima.</summary>
        public void Focar(DefinicaoDeJanela definicao)
        {
            var aberta = Procurar(definicao?.Chave);
            if (aberta != null)
            {
                Destacar(aberta);
            }
        }

        /// <summary>Fecha a janela e tira da tela. Sem efeito se ela não estiver aberta.</summary>
        public void Esconder(DefinicaoDeJanela definicao)
        {
            var aberta = Procurar(definicao?.Chave);
            if (aberta == null)
            {
                return;
            }

            _abertas.Remove(aberta);
            Desmontar(aberta);
            if (_focada == aberta)
            {
                Destacar(_abertas.Count > 0 ? _abertas[_abertas.Count - 1] : null);
            }

            if (_abertas.Count == 0 || _area.childCount == 0)
            {
                _area.RemoveFromHierarchy();
            }
        }

        /// <summary>Fecha todas as janelas.</summary>
        public void Esconder()
        {
            while (_abertas.Count > 0)
            {
                var aberta = _abertas[_abertas.Count - 1];
                _abertas.RemoveAt(_abertas.Count - 1);
                Desmontar(aberta);
            }

            _focada = null;
            _area.RemoveFromHierarchy();
        }

        private void Destacar(Aberta focada)
        {
            _focada = focada;
            foreach (var aberta in _abertas)
            {
                aberta.Painel.EnableInClassList(EstiloCore.JanelaFocada, aberta == focada);
            }
        }

        private Aberta Procurar(string chave)
        {
            if (chave == null)
            {
                return null;
            }

            foreach (var aberta in _abertas)
            {
                if (aberta.Chave == chave)
                {
                    return aberta;
                }
            }

            return null;
        }

        private static void Desmontar(Aberta aberta)
        {
            var janela = aberta.Janela;
            aberta.Janela = null;
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

            aberta.Raiz.RemoveFromHierarchy();
        }

        private static void MostrarFalha(VisualElement conteudo)
        {
            var falha = new Label(TextoFalha);
            falha.AddToClassList(EstiloCore.Mensagem);
            falha.AddToClassList(EstiloCore.MensagemErro);
            conteudo.Add(falha);
        }

        private sealed class Aberta
        {
            public Aberta(string chave, VisualElement raiz, VisualElement painel)
            {
                Chave = chave;
                Raiz = raiz;
                Painel = painel;
            }

            public string Chave { get; }
            public VisualElement Raiz { get; }
            public VisualElement Painel { get; }
            public JanelaDoJogo Janela { get; set; }
        }
    }
}
