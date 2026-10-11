#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Runtime.CompilerServices;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using UnityEngine.UIElements;

[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Desenvolvimento.Tests")]

namespace JogoDoGrupo.Client.Features.Desenvolvimento
{
    /// <summary>
    /// Caixa do console de teste. Não decide nada: manda o texto e mostra a resposta do servidor.
    /// Só entra no editor e no build de desenvolvimento.
    /// </summary>
    internal sealed class ConsoleDeTeste
    {
        private TextField _campo;
        private Label _resultado;
        private VisualElement _painel;
        private Action<string> _enviar;

        public bool Aberta { get; private set; }

        internal TextField Campo => _campo;

        internal Label Resultado => _resultado;

        public event Action EscApertado;

        public void Montar(VisualElement raiz, Action<string> enviar)
        {
            _enviar = enviar;
            _painel = new VisualElement { name = "ConsoleDeTeste" };
            _painel.AddToClassList(EstiloCore.Painel);
            _painel.AddToClassList(EstiloCore.Janela);
            _painel.style.position = Position.Absolute;
            _painel.style.top = 16;
            _painel.style.left = Length.Percent(30);
            _painel.style.width = Length.Percent(40);
            _painel.style.display = DisplayStyle.None;

            var titulo = new Label("Comandos de teste");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList(EstiloCore.JanelaTitulo);

            _campo = new TextField("Comando")
            {
                name = "ComandoDeTeste",
                maxLength = ComandosDeTeste.TextoMaximo,
            };
            _campo.AddToClassList(EstiloCore.Campo);
            _campo.RegisterCallback<KeyDownEvent>(AoTeclar);

            var botao = new Button(Enviar) { name = "EnviarComandoDeTeste", text = "Enviar" };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList(EstiloCore.BotaoPrincipal);

            _resultado = new Label(ComandosDeTeste.Ajuda) { name = "ResultadoDeComandoDeTeste" };
            _resultado.AddToClassList(EstiloCore.Mensagem);

            _painel.Add(titulo);
            _painel.Add(_campo);
            _painel.Add(botao);
            _painel.Add(_resultado);
            raiz.Add(_painel);
        }

        public void Alternar()
        {
            if (Aberta)
            {
                Fechar();
            }
            else
            {
                Abrir();
            }
        }

        public void Abrir()
        {
            Aberta = true;
            _painel.style.display = DisplayStyle.Flex;
            _campo.Focus();
        }

        public void Fechar()
        {
            Aberta = false;
            _painel.style.display = DisplayStyle.None;
        }

        public void Mostrar(ResultadoDeComandoDeTeste resultado)
        {
            if (resultado == null)
            {
                return;
            }

            _resultado.text = resultado.Texto ?? "";
            _resultado.EnableInClassList(EstiloCore.MensagemErro, !resultado.Aceito);
        }

        public void Enviar()
        {
            var texto = (_campo.value ?? "").Trim();
            if (texto.Length == 0)
            {
                return;
            }

            _enviar(texto);
            _campo.value = "";
        }

        private void AoTeclar(KeyDownEvent evt)
        {
            if (
                evt.keyCode == UnityEngine.KeyCode.Return
                || evt.keyCode == UnityEngine.KeyCode.KeypadEnter
            )
            {
                evt.StopPropagation();
                Enviar();
            }
            else if (evt.keyCode == UnityEngine.KeyCode.Escape)
            {
                evt.StopPropagation();
                EscApertado?.Invoke();
            }
        }
    }
}
#endif
