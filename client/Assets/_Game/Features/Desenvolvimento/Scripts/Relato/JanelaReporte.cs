using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Janela do report beta: abre pelo F9 ou pela tecla da barra de atalhos (J), congela o
    /// passado recente guardado pelo <see cref="GravadorDeMomento"/> e pede uma frase, o tipo, a
    /// severidade e a área. Enter (ou "Enviar") grava a pasta e manda virar issue
    /// (<see cref="EnvioDeReporte"/>); a janela fecha e o gravador volta, pronto para o próximo.
    /// Só no editor e no build de desenvolvimento.
    /// </summary>
    internal sealed class JanelaReporte : JanelaDoJogo
    {
        public const string Chave = "desenvolvimento.reporte";

        private const string Tecla = "J";
        private const int OrdemNaBarra = 900;
        private const int ReportsNoHistorico = 5;

        private static int _fecharDepoisDoQuadro = -1;

        private Momento _momento;
        private int _frameDaAbertura;
        private TextField _frase;
        private DropdownField _tipo;
        private DropdownField _severidade;
        private DropdownField _area;
        private Label _mensagem;
        private VisualElement _historico;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            if (!ComandosDeTesteNoClient.Permite)
            {
                return;
            }

            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    Chave,
                    "Report",
                    Tecla,
                    OrdemNaBarra,
                    () => new JanelaReporte()
                )
            );
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _fecharDepoisDoQuadro = -1;
        }

        /// <summary>F9: abre por cima da janela que estiver aberta, ou fecha o report.</summary>
        public static void Alternar()
        {
            if (JanelasDoJogo.ChaveAberta == Chave)
            {
                JanelasDoJogo.Fechar();
                return;
            }

            if (!BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados())
            {
                JanelasDoJogo.Abrir(Chave);
            }
        }

        /// <summary>
        /// Chamado todo quadro pelo gravador: fecha o report enviado no quadro anterior (o Enter
        /// daquele quadro não pode abrir o chat ao mesmo tempo).
        /// </summary>
        public static void Atualizar()
        {
            if (_fecharDepoisDoQuadro >= 0 && Time.frameCount > _fecharDepoisDoQuadro)
            {
                _fecharDepoisDoQuadro = -1;
                if (JanelasDoJogo.ChaveAberta == Chave)
                {
                    JanelasDoJogo.Fechar();
                }
            }
        }

        protected override void Montar(VisualElement conteudo)
        {
            var gravador = GravadorDeMomento.Instancia;
            if (gravador != null)
            {
                _momento = gravador.Congelar();
                gravador.DefinirGravando(false);
            }

            _frameDaAbertura = Time.frameCount;

            var pergunta = new Label(
                "O que aconteceu? Uma frase basta (ex.: \"posso andar enquanto morto\")."
            );
            pergunta.AddToClassList(EstiloCore.Texto);
            conteudo.Add(pergunta);

            _frase = new TextField { name = "desenvolvimento.reporte.frase", maxLength = 300 };
            _frase.AddToClassList(EstiloCore.Campo);
            _frase.RegisterCallback<KeyDownEvent>(AoApertarTecla, TrickleDown.TrickleDown);
            conteudo.Add(_frase);

            var escolhas = new VisualElement();
            escolhas.style.flexDirection = FlexDirection.Row;
            _tipo = Escolha("Tipo", Rotulos(OpcoesDoReporte.Tipos));
            _severidade = Escolha("Severidade", Rotulos(OpcoesDoReporte.Severidades));
            _area = Escolha("Área", new List<string>(OpcoesDoReporte.Areas));
            escolhas.Add(_tipo);
            escolhas.Add(_severidade);
            escolhas.Add(_area);
            conteudo.Add(escolhas);

            var resumo = new Label(Resumo());
            resumo.AddToClassList(EstiloCore.Dica);
            conteudo.Add(resumo);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Dica);
            conteudo.Add(_mensagem);

            var botoes = new VisualElement();
            botoes.AddToClassList(EstiloCore.LinhaBotoes);
            botoes.Add(Botao("Enviar", Enviar, true));
            botoes.Add(Botao("Cancelar", () => JanelasDoJogo.Fechar(), false));
            conteudo.Add(botoes);

            _historico = new VisualElement();
            conteudo.Add(_historico);
            MostrarHistorico();
            EnvioDeReporte.Mudou += MostrarHistorico;

            _frase.schedule.Execute(() => _frase.Focus());
        }

        protected override void AoFechar()
        {
            EnvioDeReporte.Mudou -= MostrarHistorico;
            _momento?.Liberar();
            _momento = null;
            GravadorDeMomento.Instancia?.DefinirGravando(true);
        }

        private string Resumo()
        {
            if (_momento == null)
            {
                return "O gravador está desligado: o report vai só com a frase.";
            }

            var quadros = _momento.Quadros;
            var segundos =
                quadros.Count > 1 ? quadros[quadros.Count - 1].Tempo - quadros[0].Tempo : 0;
            return $"Guardado: {segundos:0.0} s de linha do tempo, {_momento.Texturas.Count} quadros com imagem, "
                + $"{_momento.Eventos.Count} mensagens. Enter envia como issue, Esc cancela.";
        }

        private void Enviar()
        {
            // O Enter de outra caixa não pode enviar no mesmo quadro em que esta abriu.
            if (Time.frameCount == _frameDaAbertura || _fecharDepoisDoQuadro >= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_frase.value))
            {
                _mensagem.text = "Escreva o que aconteceu antes de enviar.";
                _frase.Focus();
                return;
            }

            if (_momento == null)
            {
                _mensagem.text = "Sem gravador nesta sessão: não dá para gravar o report.";
                return;
            }

            var dados = new DadosDoReporte
            {
                Frase = _frase.value.Trim(),
                Tipo = OpcoesDoReporte.Valor(OpcoesDoReporte.Tipos, _tipo.value),
                Severidade = OpcoesDoReporte.Valor(OpcoesDoReporte.Severidades, _severidade.value),
                Area = _area.value == OpcoesDoReporte.AreaNaoSei ? "" : _area.value,
            };

            try
            {
                var pasta = EscritorDeRelato.Escrever(_momento, dados);
                Debug.Log($"[Relato] Report salvo em {pasta}");
                EnvioDeReporte.Registrar(System.IO.Path.GetFileName(pasta), dados.Frase);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _mensagem.text = "Não deu para salvar o report (veja o console).";
                return;
            }

            _fecharDepoisDoQuadro = Time.frameCount;
        }

        private void MostrarHistorico()
        {
            if (!Aberta)
            {
                return;
            }

            _historico.Clear();
            var itens = EnvioDeReporte.Historico;
            if (itens.Count == 0)
            {
                return;
            }

            var titulo = new Label("Reports desta sessão");
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _historico.Add(titulo);
            for (var i = itens.Count - 1; i >= 0 && i >= itens.Count - ReportsNoHistorico; i--)
            {
                var linha = new Label($"{itens[i].Frase} · {itens[i].Situacao}");
                linha.AddToClassList(EstiloCore.Dica);
                _historico.Add(linha);
            }
        }

        private void AoApertarTecla(KeyDownEvent evento)
        {
            switch (evento.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evento.StopPropagation();
                    Enviar();
                    break;
                case KeyCode.Escape:
                    evento.StopPropagation();
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = true;
                    JanelasDoJogo.Fechar();
                    break;
            }
        }

        private static List<string> Rotulos((string Rotulo, string Valor)[] opcoes)
        {
            var rotulos = new List<string>(opcoes.Length);
            foreach (var opcao in opcoes)
            {
                rotulos.Add(opcao.Rotulo);
            }

            return rotulos;
        }

        private static DropdownField Escolha(string rotulo, List<string> opcoes)
        {
            var escolha = new DropdownField(rotulo, opcoes, 0);
            escolha.AddToClassList(EstiloCore.Campo);
            escolha.style.flexGrow = 1;
            escolha.style.flexBasis = 0;
            return escolha;
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
    }
}
