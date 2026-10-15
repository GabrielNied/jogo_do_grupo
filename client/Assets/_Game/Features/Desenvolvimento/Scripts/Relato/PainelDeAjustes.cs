using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// O painel de ajustes de sensação (F10), montado em código por cima de tudo. Aberto, o
    /// gravador para, o personagem fica parado e o cursor fica livre, como no chat. Esc fecha.
    /// O report (F9 ou J) é a <see cref="JanelaReporte"/>.
    /// </summary>
    internal sealed class PainelDeAjustes
    {
        private const float OrdemDoPainel = 50f;
        private const float LarguraDoPainel = 560f;

        private readonly GravadorDeMomento _gravador;
        private readonly List<Action> _atualizacoes = new List<Action>();

        private GameObject _objeto;
        private VisualElement _raiz;
        private VisualElement _caixaAjustes;
        private Label _mensagemAjustes;

        public PainelDeAjustes(GravadorDeMomento gravador)
        {
            _gravador = gravador;
            Sintonia.Mudou += AtualizarAjustes;
        }

        public bool Aberto =>
            _caixaAjustes != null && _caixaAjustes.style.display == DisplayStyle.Flex;

        public void Alternar()
        {
            if (Aberto)
            {
                Fechar();
                return;
            }

            if (JanelasDoJogo.AlgoAberto || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados())
            {
                return;
            }

            Montar();
            MontarAjustes();
            _gravador.DefinirGravando(false);
            _caixaAjustes.style.display = DisplayStyle.Flex;
            PrenderJogo(true);
        }

        public void Destruir()
        {
            Sintonia.Mudou -= AtualizarAjustes;
            if (_objeto != null)
            {
                UnityEngine.Object.Destroy(_objeto);
            }
        }

        private void Fechar()
        {
            if (_caixaAjustes != null)
            {
                _caixaAjustes.style.display = DisplayStyle.None;
            }

            _gravador.DefinirGravando(true);
            PrenderJogo(false);
        }

        /// <summary>
        /// Chamado todo quadro pelo gravador: fecha o painel no Esc (ele não tem campo de texto
        /// para receber a tecla).
        /// </summary>
        public void Atualizar(bool escApertado)
        {
            // A sessão já ignorou este Esc (o painel aberto bloqueia os atalhos); não marca
            // como consumido, senão o próximo Esc da pessoa se perde.
            if (escApertado && Aberto)
            {
                Fechar();
            }
        }

        private void Montar()
        {
            if (_raiz != null)
            {
                return;
            }

            _objeto = new GameObject("Desenvolvimento.Ajustes.Painel");
            UnityEngine.Object.DontDestroyOnLoad(_objeto);
            _objeto.SetActive(false);
            var documento = _objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelAjustes",
                OrdemDoPainel
            );
            _objeto.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);

            _raiz = new VisualElement { name = "desenvolvimento.ajustes" };
            _raiz.AddToClassList(EstiloCore.RaizInterface);
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.style.alignItems = Align.Center;
            _raiz.style.justifyContent = Justify.Center;
            documento.rootVisualElement.Add(_raiz);

            _caixaAjustes = Caixa("Ajustes de sensação (F10)");
            _raiz.Add(_caixaAjustes);
        }

        private void MontarAjustes()
        {
            _caixaAjustes.Clear();
            _atualizacoes.Clear();
            var titulo = new Label("Ajustes de sensação (F10)");
            titulo.AddToClassList(EstiloCore.Titulo);
            _caixaAjustes.Add(titulo);

            var lista = new ScrollView(ScrollViewMode.Vertical);
            lista.style.maxHeight = new Length(60, LengthUnit.Percent);
            string grupo = null;
            foreach (var ajuste in Sintonia.Ajustes)
            {
                if (ajuste.Grupo != grupo)
                {
                    grupo = ajuste.Grupo;
                    var subtitulo = new Label(grupo);
                    subtitulo.AddToClassList(EstiloCore.Subtitulo);
                    lista.Add(subtitulo);
                }

                lista.Add(Linha(ajuste));
            }

            if (Sintonia.Ajustes.Count == 0)
            {
                var vazio = new Label("Nenhum valor registrado na Sintonia.");
                vazio.AddToClassList(EstiloCore.Texto);
                lista.Add(vazio);
            }

            _caixaAjustes.Add(lista);
            _mensagemAjustes = new Label("Mude e jogue; os valores valem até sair do jogo.");
            _mensagemAjustes.AddToClassList(EstiloCore.Dica);
            _caixaAjustes.Add(_mensagemAjustes);

            var botoes = new VisualElement();
            botoes.AddToClassList(EstiloCore.LinhaBotoes);
            botoes.Add(Botao("Copiar valores", Copiar, true));
            botoes.Add(Botao("Restaurar tudo", Sintonia.RestaurarTodos, false));
            botoes.Add(Botao("Fechar", Fechar, false));
            _caixaAjustes.Add(botoes);
        }

        private VisualElement Linha(AjusteDeSensacao ajuste)
        {
            var linha = new VisualElement();
            linha.style.marginBottom = 6;

            var cabecalho = new VisualElement();
            cabecalho.style.flexDirection = FlexDirection.Row;
            var nome = new Label(ajuste.Nome);
            nome.AddToClassList(EstiloCore.Texto);
            nome.style.flexGrow = 1;
            var restaurar = Botao("↺", ajuste.Restaurar, false);
            cabecalho.Add(nome);
            cabecalho.Add(restaurar);
            linha.Add(cabecalho);

            var controles = new VisualElement();
            controles.style.flexDirection = FlexDirection.Row;
            var slider = new Slider(ajuste.Minimo, ajuste.Maximo) { value = ajuste.Valor };
            slider.style.flexGrow = 1;
            slider.RegisterValueChangedCallback(e => ajuste.Valor = e.newValue);
            var campo = new FloatField { value = ajuste.Valor };
            campo.style.width = 90;
            campo.AddToClassList(EstiloCore.Campo);
            campo.RegisterValueChangedCallback(e => ajuste.Valor = e.newValue);
            controles.Add(slider);
            controles.Add(campo);
            linha.Add(controles);

            var descricao = new Label(
                $"{ajuste.Descricao} · padrão {Sintonia.Formatar(ajuste.Padrao)} · de {Sintonia.Formatar(ajuste.Minimo)} a {Sintonia.Formatar(ajuste.Maximo)}"
            );
            descricao.AddToClassList(EstiloCore.Dica);
            linha.Add(descricao);

            void Atualizar()
            {
                slider.SetValueWithoutNotify(ajuste.Valor);
                campo.SetValueWithoutNotify(ajuste.Valor);
                nome.text = ajuste.Alterado ? ajuste.Nome + " •" : ajuste.Nome;
                nome.EnableInClassList(EstiloCore.TextoGanho, ajuste.Alterado);
            }

            _atualizacoes.Add(Atualizar);
            Atualizar();
            return linha;
        }

        private void AtualizarAjustes()
        {
            foreach (var atualizar in _atualizacoes)
            {
                atualizar();
            }
        }

        private void Copiar()
        {
            var alterados = Sintonia.Alterados();
            if (alterados.Length == 0)
            {
                _mensagemAjustes.text = "Nada mudou do padrão.";
                return;
            }

            GUIUtility.systemCopyBuffer = alterados;
            var caminho = EscritorDeRelato.GravarAjustes(alterados);
            _mensagemAjustes.text =
                $"Copiado e salvo em {caminho}. Cole para a IA gravar no código.";
            Debug.Log("[Relato] Ajustes fora do padrão:\n" + alterados);
        }

        private static VisualElement Caixa(string titulo)
        {
            var caixa = new VisualElement();
            caixa.AddToClassList(EstiloCore.Painel);
            caixa.style.width = LarguraDoPainel;
            caixa.style.display = DisplayStyle.None;
            var rotulo = new Label(titulo);
            rotulo.AddToClassList(EstiloCore.Titulo);
            caixa.Add(rotulo);
            return caixa;
        }

        private static Button Botao(string texto, Action aoClicar, bool principal)
        {
            var botao = new Button(aoClicar) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            if (principal)
            {
                botao.AddToClassList(EstiloCore.BotaoPrincipal);
            }

            return botao;
        }

        // Com o painel aberto o personagem e a câmera param e o cursor fica livre (o ataque só
        // sai com o cursor preso), como no chat.
        private static void PrenderJogo(bool painelAberto)
        {
            var jogador = EntradaNoJogo.Jogador;
            var controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (controle != null)
            {
                controle.InputAtivo = !painelAberto;
            }

            var camera = Camera.main;
            var controleDaCamera =
                camera != null ? camera.GetComponent<CameraTerceiraPessoa>() : null;
            if (controleDaCamera != null)
            {
                controleDaCamera.InputAtivo = !painelAberto;
            }

            if (painelAberto)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else if (!JanelasDoJogo.AlgoAberto)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }
    }
}
