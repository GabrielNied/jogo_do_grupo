using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Quando o painel fica quase transparente: sem atividade (mensagem nova ou caixa aberta)
    /// por <see cref="SegundosParaEsmaecer"/>. A caixa aberta nunca esmaece.
    /// </summary>
    internal sealed class AtividadeDoPainel
    {
        public const double SegundosParaEsmaecer = 10.0;

        private double _ultima;

        public AtividadeDoPainel(double agora)
        {
            _ultima = agora;
        }

        public void Registrar(double agora) => _ultima = agora;

        public bool Esmaecido(double agora, bool caixaAberta) =>
            !caixaAberta && agora - _ultima >= SegundosParaEsmaecer;
    }

    /// <summary>
    /// O painel do chat (UI Toolkit montada em código): as últimas linhas, com o texto sempre
    /// puro, e a caixa de texto. Só desenha e repassa Enter e Esc; cores e tamanhos estão em
    /// <c>EstiloSocial.uss</c>.
    /// </summary>
    internal sealed class PainelDeChat
    {
        public const int TamanhoMaximoDaCaixa = 400;
        public const string CaminhoEstilo = "UI/EstiloSocial";

        private readonly VisualElement _raiz;
        private const string ChaveOculto = "lumora.chat.oculto";
        private readonly ScrollView _rolagem;
        private readonly VisualElement _conteudo;
        private readonly VisualElement _painel;
        private readonly Button _abrirPainel;
        private readonly Button _irAoFim;
        private readonly DropdownField _filtro;
        private readonly TextField _campo;
        private readonly List<Button> _abas = new List<Button>();
        private bool _primeiraRenderizacao = true;

        public PainelDeChat(VisualElement regiao)
        {
            _raiz = new VisualElement { name = "social.chat" };
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList("chat");
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                _raiz.styleSheets.Add(estilo);
            }

            _abrirPainel = new Button(() => DefinirOculto(false)) { text = "Chat" };
            _abrirPainel.AddToClassList(EstiloCore.Botao);
            _abrirPainel.AddToClassList("chat-abrir");
            _raiz.Add(_abrirPainel);

            _painel = new VisualElement();
            _painel.AddToClassList("chat-painel");

            // Abas como as do World of Warcraft; o seletor continua sendo a fonte do filtro.
            var nomesDasAbas = new List<string>
            {
                "Geral",
                "Cidade",
                "Grupo",
                "Sussurro",
                "Sistema",
            };
            _filtro = new DropdownField(nomesDasAbas, 0);
            _filtro.name = "social.chat.filtro";
            _filtro.AddToClassList("chat-filtro");
            _filtro.RegisterValueChangedCallback(_ =>
            {
                AtualizarAbas();
                ConfiguracaoMudou?.Invoke();
            });

            var cabecalho = new VisualElement();
            cabecalho.AddToClassList("chat-cabecalho");
            for (var i = 0; i < nomesDasAbas.Count; i++)
            {
                var indice = i;
                var aba = new Button(() => _filtro.index = indice) { text = nomesDasAbas[i] };
                aba.AddToClassList("chat-aba");
                _abas.Add(aba);
                cabecalho.Add(aba);
            }

            var espaco = new VisualElement();
            espaco.AddToClassList("chat-espaco");
            cabecalho.Add(espaco);
            cabecalho.Add(_filtro);
            var ocultar = new Button(() => DefinirOculto(true)) { text = "−" };
            ocultar.AddToClassList("chat-ocultar");
            cabecalho.Add(ocultar);
            _painel.Add(cabecalho);
            AtualizarAbas();

            var opcoes = new VisualElement();
            opcoes.AddToClassList("chat-opcoes");
            var sistema = new Toggle("Sistema") { value = PreferenciasDeAvisos.MostrarSistema };
            sistema.name = "social.chat.sistema";
            sistema.RegisterValueChangedCallback(e =>
            {
                PreferenciasDeAvisos.MostrarSistema = e.newValue;
                ConfiguracaoMudou?.Invoke();
            });
            var combate = new Toggle("Combate") { value = PreferenciasDeAvisos.MostrarCombate };
            combate.name = "social.chat.combate";
            combate.RegisterValueChangedCallback(e =>
            {
                PreferenciasDeAvisos.MostrarCombate = e.newValue;
                ConfiguracaoMudou?.Invoke();
            });
            opcoes.Add(sistema);
            opcoes.Add(combate);
            _painel.Add(opcoes);

            _rolagem = new ScrollView(ScrollViewMode.Vertical) { name = "social.chat.historico" };
            _rolagem.AddToClassList("chat-linhas");
            _conteudo = _rolagem.contentContainer;
            _painel.Add(_rolagem);
            _irAoFim = new Button(IrAoFim) { text = "▼" };
            _irAoFim.AddToClassList("chat-ir-ao-fim");
            _rolagem.verticalScroller.valueChanged += _ =>
                _irAoFim.style.display =
                    _rolagem.verticalScroller.value < _rolagem.verticalScroller.highValue - 12f
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            _painel.Add(_irAoFim);

            _campo = new TextField { name = "social.chat.campo", maxLength = TamanhoMaximoDaCaixa };
            _campo.AddToClassList("chat-campo");
            _campo.style.display = DisplayStyle.None;
            _campo.RegisterCallback<KeyDownEvent>(AoApertarTecla, TrickleDown.TrickleDown);

            _painel.Add(_campo);
            _raiz.Add(_painel);
            regiao.Add(_raiz);
            DefinirOculto(PlayerPrefs.GetInt(ChaveOculto, 0) != 0, gravar: false);
        }

        private void AtualizarAbas()
        {
            for (var i = 0; i < _abas.Count; i++)
                _abas[i].EnableInClassList("chat-aba-ativa", i == _filtro.index);
        }

        /// <summary>Enter apertado com a caixa aberta.</summary>
        public event Action EnterApertado;

        /// <summary>Esc apertado com a caixa aberta.</summary>
        public event Action EscApertado;

        /// <summary>Tab: devolve o texto completo sugerido ou null.</summary>
        public event Func<string, string> CompletarTexto;

        public event Action ConfiguracaoMudou;

        public FiltroDoChat Filtro => (FiltroDoChat)_filtro.index;

        public bool Aberta => _campo.style.display == DisplayStyle.Flex;

        public string Texto
        {
            get => _campo.value ?? "";
            set => _campo.SetValueWithoutNotify(value ?? "");
        }

        public VisualElement Raiz => _raiz;

        public void Abrir()
        {
            DefinirOculto(false);
            _campo.style.display = DisplayStyle.Flex;
            // O foco só vale depois que o campo entra no layout.
            _campo.schedule.Execute(() => _campo.Q("unity-text-input")?.Focus()).StartingIn(0);
        }

        public void Fechar()
        {
            Texto = "";
            _campo.style.display = DisplayStyle.None;
            _campo.Blur();
        }

        public void Esmaecer(bool esmaecido)
        {
            _painel.EnableInClassList("chat-esmaecido", esmaecido);
        }

        public void RolarPagina(bool paraCima)
        {
            _rolagem.scrollOffset += new Vector2(0, (paraCima ? -1 : 1) * _rolagem.layout.height);
        }

        private void DefinirOculto(bool oculto, bool gravar = true)
        {
            if (oculto && Aberta)
            {
                EscApertado?.Invoke();
            }

            _painel.style.display = oculto ? DisplayStyle.None : DisplayStyle.Flex;
            _abrirPainel.style.display = oculto ? DisplayStyle.Flex : DisplayStyle.None;
            if (gravar)
            {
                PlayerPrefs.SetInt(ChaveOculto, oculto ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void IrAoFim()
        {
            _rolagem.scrollOffset = new Vector2(0, float.MaxValue);
            _rolagem.schedule.Execute(() =>
            {
                _rolagem.scrollOffset = new Vector2(0, _rolagem.verticalScroller.highValue);
                _irAoFim.style.display = DisplayStyle.None;
            });
        }

        /// <summary>Mostra até 100 linhas filtradas e mantém a rolagem quando se lê o histórico.</summary>
        public void Mostrar(IReadOnlyList<LinhaDeChat> linhas, TimeSpan fuso)
        {
            var noFim =
                _primeiraRenderizacao
                || _rolagem.verticalScroller.value >= _rolagem.verticalScroller.highValue - 12f;
            _primeiraRenderizacao = false;
            _conteudo.Clear();
            if (linhas.Count == 0)
            {
                var vazio = new Label("Nenhuma mensagem neste filtro.");
                vazio.AddToClassList("chat-vazio");
                _conteudo.Add(vazio);
            }
            foreach (var linha in linhas)
            {
                var rotulo = new Label { enableRichText = false };
                rotulo.AddToClassList("chat-linha");
                rotulo.text = TextosDeChat.Linha(linha, fuso);
                rotulo.EnableInClassList("chat-cidade", linha.Canal == CanalDaLinha.Cidade);
                rotulo.EnableInClassList("chat-grupo", linha.Canal == CanalDaLinha.Grupo);
                rotulo.EnableInClassList("chat-sussurro", linha.Canal == CanalDaLinha.Sussurro);
                rotulo.EnableInClassList("chat-sistema", linha.Canal == CanalDaLinha.Sistema);
                _conteudo.Add(rotulo);
            }
            if (noFim)
                IrAoFim();
            _irAoFim.style.display = noFim ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void AoApertarTecla(KeyDownEvent evento)
        {
            switch (evento.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evento.StopPropagation();
                    EnterApertado?.Invoke();
                    break;
                case KeyCode.Escape:
                    evento.StopPropagation();
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = true;
                    EscApertado?.Invoke();
                    break;
                case KeyCode.Tab:
                    evento.StopPropagation();
                    TentarCompletar();
                    break;
            }
        }

        private void TentarCompletar()
        {
            var sugestao = CompletarTexto?.Invoke(Texto);
            if (string.IsNullOrEmpty(sugestao))
            {
                return;
            }

            Texto = sugestao;
        }
    }
}
