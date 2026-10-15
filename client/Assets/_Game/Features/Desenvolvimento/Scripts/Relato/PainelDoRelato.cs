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
    /// A caixa do relato (F9) e o painel de ajustes (F10), montados em código por cima de tudo.
    /// Com um deles aberto o gravador para, o personagem fica parado e o cursor fica livre, como
    /// no chat. Enter salva o relato, Esc fecha sem salvar.
    /// </summary>
    internal sealed class PainelDoRelato
    {
        private const float OrdemDoPainel = 50f;
        private const float LarguraDoPainel = 560f;

        private readonly GravadorDeMomento _gravador;
        private readonly List<Action> _atualizacoes = new List<Action>();

        private GameObject _objeto;
        private VisualElement _raiz;
        private VisualElement _caixaRelato;
        private TextField _frase;
        private Label _resumo;
        private VisualElement _caixaAjustes;
        private Label _mensagemAjustes;
        private Momento _momento;
        private int _frameDaAbertura;
        private int _fecharDepoisDoQuadro = -1;

        public PainelDoRelato(GravadorDeMomento gravador)
        {
            _gravador = gravador;
            Sintonia.Mudou += AtualizarAjustes;
        }

        public bool Aberto => RelatoAberto || AjustesAberto;

        private bool RelatoAberto =>
            _caixaRelato != null && _caixaRelato.style.display == DisplayStyle.Flex;

        private bool AjustesAberto =>
            _caixaAjustes != null && _caixaAjustes.style.display == DisplayStyle.Flex;

        public void AlternarRelato()
        {
            if (RelatoAberto)
            {
                Fechar();
                return;
            }

            if (JanelasDoJogo.AlgoAberto || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados())
            {
                return;
            }

            Montar();
            FecharAjustes();
            _momento = _gravador.Congelar();
            _gravador.DefinirGravando(false);
            var segundos =
                _momento.Quadros.Count > 1
                    ? _momento.Quadros[_momento.Quadros.Count - 1].Tempo - _momento.Quadros[0].Tempo
                    : 0;
            _resumo.text =
                $"{_momento.Quadros.Count} quadros, {segundos:0.0} s, {_momento.Eventos.Count} mensagens. Enter salva, Esc descarta.";
            _frase.value = "";
            _caixaRelato.style.display = DisplayStyle.Flex;
            _frameDaAbertura = Time.frameCount;
            PrenderJogo(true);
            _frase.schedule.Execute(() => _frase.Focus());
        }

        public void AlternarAjustes()
        {
            if (AjustesAberto)
            {
                Fechar();
                return;
            }

            if (
                RelatoAberto
                || JanelasDoJogo.AlgoAberto
                || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados()
            )
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
            if (_caixaRelato != null)
            {
                _caixaRelato.style.display = DisplayStyle.None;
            }

            FecharAjustes();
            _momento = null;
            _fecharDepoisDoQuadro = -1;
            _gravador.DefinirGravando(true);
            PrenderJogo(false);
        }

        private void FecharAjustes()
        {
            if (_caixaAjustes != null)
            {
                _caixaAjustes.style.display = DisplayStyle.None;
            }
        }

        private void Salvar()
        {
            // O Enter de outra caixa não pode salvar no mesmo quadro em que esta abriu.
            if (Time.frameCount == _frameDaAbertura || _momento == null)
            {
                return;
            }

            try
            {
                var pasta = EscritorDeRelato.Escrever(_momento, _frase.value);
                Debug.Log($"[Relato] Salvo em {pasta}");
                AvisosDoJogo.Mostrar("Relato salvo em relatos/. Peça à IA: /relato");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AvisosDoJogo.Mostrar(
                    "Não deu para salvar o relato (veja o console).",
                    TipoDeAviso.Erro
                );
            }

            // Fecha no próximo quadro: o Enter deste quadro não pode abrir o chat ao mesmo tempo.
            _momento = null;
            _fecharDepoisDoQuadro = Time.frameCount;
        }

        /// <summary>
        /// Chamado todo quadro pelo gravador: fecha o relato salvo no quadro anterior e fecha o
        /// painel no Esc (o painel de ajustes não tem campo de texto para receber a tecla).
        /// </summary>
        public void Atualizar(bool escApertado)
        {
            if (_fecharDepoisDoQuadro >= 0 && Time.frameCount > _fecharDepoisDoQuadro)
            {
                Fechar();
                return;
            }

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

            _objeto = new GameObject("Desenvolvimento.Relato.Painel");
            UnityEngine.Object.DontDestroyOnLoad(_objeto);
            _objeto.SetActive(false);
            var documento = _objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelRelato",
                OrdemDoPainel
            );
            _objeto.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);

            _raiz = new VisualElement { name = "desenvolvimento.relato" };
            _raiz.AddToClassList(EstiloCore.RaizInterface);
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.style.alignItems = Align.Center;
            _raiz.style.justifyContent = Justify.Center;
            documento.rootVisualElement.Add(_raiz);

            _caixaRelato = Caixa("Relato de sensação (F9)");
            var pergunta = new Label(
                "O que pareceu errado? Uma frase basta (ex.: \"o pesado não tem peso\")."
            );
            pergunta.AddToClassList(EstiloCore.Texto);
            _caixaRelato.Add(pergunta);
            _frase = new TextField { name = "desenvolvimento.relato.frase", maxLength = 300 };
            _frase.AddToClassList(EstiloCore.Campo);
            _frase.RegisterCallback<KeyDownEvent>(AoApertarTecla, TrickleDown.TrickleDown);
            _caixaRelato.Add(_frase);
            _resumo = new Label();
            _resumo.AddToClassList(EstiloCore.Dica);
            _caixaRelato.Add(_resumo);
            var botoes = new VisualElement();
            botoes.AddToClassList(EstiloCore.LinhaBotoes);
            botoes.Add(Botao("Salvar", Salvar, true));
            botoes.Add(Botao("Descartar", Fechar, false));
            _caixaRelato.Add(botoes);
            _raiz.Add(_caixaRelato);

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

        private void AoApertarTecla(KeyDownEvent evento)
        {
            switch (evento.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evento.StopPropagation();
                    Salvar();
                    break;
                case KeyCode.Escape:
                    evento.StopPropagation();
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = true;
                    Fechar();
                    break;
            }
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
