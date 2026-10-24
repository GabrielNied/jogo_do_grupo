using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
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
        private readonly PosicoesDeJanelas _posicoes;

        // Na ordem em que foram mostradas; a focada é a de cima.
        private readonly List<Aberta> _abertas = new List<Aberta>();
        private Aberta _focada;

        /// <param name="container">Elemento que recebe as janelas (o das telas do Core).</param>
        /// <param name="aoPedirFechar">Chamado com a chave quando a pessoa clica no X.</param>
        /// <param name="aoFocar">Chamado com a chave quando a pessoa clica numa janela.</param>
        /// <param name="posicoes">Onde cada janela arrastada ficou (padrão: PlayerPrefs).</param>
        public ApresentadorDeJanelas(
            VisualElement container,
            Action<string> aoPedirFechar,
            Action<string> aoFocar = null,
            PosicoesDeJanelas posicoes = null
        )
        {
            _container = container;
            _aoPedirFechar = aoPedirFechar;
            _aoFocar = aoFocar;
            _posicoes = posicoes ?? new PosicoesDeJanelas();
            _area = new VisualElement { name = NomeArea, pickingMode = PickingMode.Ignore };
            _area.AddToClassList(EstiloCore.JanelasArea);
            // Tela mudou de tamanho com janelas soltas: cada uma volta para dentro da vista.
            _area.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                foreach (var aberta in _abertas)
                {
                    PrenderNaArea(aberta);
                }
            });
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

            // A moldura não rola: cabeçalho (com o X) e rodapé ficam fixos e só o conteúdo rola.
            VisualElement painel = telaInteira ? new VisualElement() : ComponentesVisuais.Painel();
            if (!telaInteira)
            {
                painel.AddToClassList(EstiloCore.Painel);
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
                var rolagem = new ScrollView(ScrollViewMode.Vertical)
                {
                    horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                    verticalScrollerVisibility = ScrollerVisibility.Auto,
                };
                rolagem.AddToClassList(EstiloCore.JanelaRolagem);
                EventCallback<GeometryChangedEvent> conferirBarra = _ =>
                    rolagem.EnableInClassList(
                        EstiloCore.JanelaRolagemComBarra,
                        BarraVisivel(rolagem.verticalScroller)
                    );
                rolagem.contentViewport.RegisterCallback(conferirBarra);
                rolagem.verticalScroller.RegisterCallback(conferirBarra);
                rolagem.Add(conteudo);
                painel.Add(cabecalho);
                painel.Add(rolagem);
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
            if (!telaInteira)
            {
                PermitirArrastar(aberta, cabecalho, fechar);
                VoltarAPosicaoGravada(aberta);
            }

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

        /// <summary>
        /// Se a janela está solta (arrastada ou reaberta onde a pessoa deixou) e onde está, no
        /// espaço da área das janelas. Para testes e diagnóstico.
        /// </summary>
        internal bool TentarPosicao(string chave, out Vector2 posicao)
        {
            var aberta = Procurar(chave);
            posicao = aberta != null ? aberta.Posicao : default;
            return aberta != null && aberta.Solta;
        }

        private static bool BarraVisivel(VisualElement barra)
        {
            return barra != null
                && barra.resolvedStyle.display != DisplayStyle.None
                && barra.resolvedStyle.visibility == Visibility.Visible;
        }

        // Arrasta pelo cabeçalho (fora do X). Abaixo do limiar o clique só traz a janela para
        // cima; ao soltar, a posição fica gravada para a próxima vez que ela abrir.
        private void PermitirArrastar(Aberta aberta, VisualElement cabecalho, VisualElement fechar)
        {
            cabecalho.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (
                    evt.button != 0
                    || (evt.target is VisualElement alvo && fechar.Contains(alvo))
                    || cabecalho.HasPointerCapture(evt.pointerId)
                )
                {
                    return;
                }

                aberta.Arrasto = new Arrasto(
                    evt.pointerId,
                    evt.position,
                    aberta.Solta ? aberta.Posicao : aberta.Raiz.layout.position
                );
                cabecalho.CapturePointer(evt.pointerId);
            });

            cabecalho.RegisterCallback<PointerMoveEvent>(evt =>
            {
                var arrasto = aberta.Arrasto;
                if (arrasto == null || arrasto.PonteiroId != evt.pointerId)
                {
                    return;
                }

                var deslocamento = (Vector2)evt.position - arrasto.InicioDoPonteiro;
                if (!arrasto.Arrastando)
                {
                    if (deslocamento.magnitude < ConfigJanelas.LimiarDeArrasto)
                    {
                        return;
                    }

                    if (!Valido(arrasto.InicioDaJanela))
                    {
                        return;
                    }

                    arrasto.Arrastando = true;
                    aberta.Raiz.BringToFront();
                }

                Posicionar(aberta, Limitada(aberta, arrasto.InicioDaJanela + deslocamento));
                evt.StopPropagation();
            });

            cabecalho.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (aberta.Arrasto == null || aberta.Arrasto.PonteiroId != evt.pointerId)
                {
                    return;
                }

                if (cabecalho.HasPointerCapture(evt.pointerId))
                {
                    cabecalho.ReleasePointer(evt.pointerId);
                }

                TerminarArrasto(aberta);
            });

            cabecalho.RegisterCallback<PointerCaptureOutEvent>(_ => TerminarArrasto(aberta));
        }

        private void TerminarArrasto(Aberta aberta)
        {
            var arrasto = aberta.Arrasto;
            aberta.Arrasto = null;
            if (arrasto == null || !arrasto.Arrastando)
            {
                return;
            }

            var area = _area.layout.size;
            if (Valido(area) && area.x > 0f && area.y > 0f)
            {
                _posicoes.Gravar(aberta.Chave, aberta.Posicao, area);
            }
        }

        // Reabre onde a pessoa deixou. Aplica já (sem piscar no lugar padrão) e confere no
        // primeiro layout: se a área mudou de tamanho (outra resolução), volta ao padrão.
        private void VoltarAPosicaoGravada(Aberta aberta)
        {
            if (!_posicoes.TentarLerGravada(aberta.Chave, out var posicao, out var areaGravada))
            {
                return;
            }

            Posicionar(aberta, posicao);
            aberta.AreaGravada = areaGravada;
            aberta.Raiz.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (aberta.AreaGravada.HasValue)
                {
                    var area = _area.layout.size;
                    if (!Valido(area) || area.x <= 0f || area.y <= 0f)
                    {
                        return;
                    }

                    var mesma = PosicoesDeJanelas.MesmaArea(area, aberta.AreaGravada.Value);
                    aberta.AreaGravada = null;
                    if (!mesma)
                    {
                        VoltarAoPadrao(aberta);
                        return;
                    }
                }

                PrenderNaArea(aberta);
            });
        }

        private void PrenderNaArea(Aberta aberta)
        {
            if (!aberta.Solta || aberta.AreaGravada.HasValue || aberta.Arrasto?.Arrastando == true)
            {
                return;
            }

            var limitada = Limitada(aberta, aberta.Posicao);
            if (limitada != aberta.Posicao)
            {
                Posicionar(aberta, limitada);
            }
        }

        private Vector2 Limitada(Aberta aberta, Vector2 posicao)
        {
            var area = _area.layout.size;
            if (!Valido(area) || area.x <= 0f || area.y <= 0f)
            {
                return posicao;
            }

            var tamanho = aberta.Raiz.layout.size;
            return PosicoesDeJanelas.Limitar(posicao, Valido(tamanho) ? tamanho : default, area);
        }

        private static void Posicionar(Aberta aberta, Vector2 posicao)
        {
            aberta.Solta = true;
            aberta.Posicao = posicao;
            aberta.Raiz.AddToClassList(EstiloCore.JanelaSolta);
            aberta.Raiz.style.left = posicao.x;
            aberta.Raiz.style.top = posicao.y;
        }

        private static void VoltarAoPadrao(Aberta aberta)
        {
            aberta.Solta = false;
            aberta.Posicao = default;
            aberta.Raiz.RemoveFromClassList(EstiloCore.JanelaSolta);
            aberta.Raiz.style.left = StyleKeyword.Null;
            aberta.Raiz.style.top = StyleKeyword.Null;
        }

        private static bool Valido(Vector2 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y);
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

            // Arrastada ou reaberta onde a pessoa deixou: fora da fila lado a lado.
            public bool Solta { get; set; }
            public Vector2 Posicao { get; set; }

            // Tamanho da área em que a posição foi gravada, até o primeiro layout conferir.
            public Vector2? AreaGravada { get; set; }
            public Arrasto Arrasto { get; set; }
        }

        private sealed class Arrasto
        {
            public Arrasto(int ponteiroId, Vector2 inicioDoPonteiro, Vector2 inicioDaJanela)
            {
                PonteiroId = ponteiroId;
                InicioDoPonteiro = inicioDoPonteiro;
                InicioDaJanela = inicioDaJanela;
            }

            public int PonteiroId { get; }
            public Vector2 InicioDoPonteiro { get; }
            public Vector2 InicioDaJanela { get; }
            public bool Arrastando { get; set; }
        }
    }
}
