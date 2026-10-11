using System;
using System.Collections.Generic;
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
        public const int LinhasVisiveis = 8;
        public const int TamanhoMaximoDaCaixa = 400;
        public const string CaminhoEstilo = "UI/EstiloSocial";

        private readonly VisualElement _raiz;
        private readonly List<Label> _rotulos = new List<Label>();
        private readonly TextField _campo;

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

            var linhas = new VisualElement { name = "social.chat.linhas" };
            linhas.pickingMode = PickingMode.Ignore;
            linhas.AddToClassList("chat-linhas");
            for (var i = 0; i < LinhasVisiveis; i++)
            {
                // Texto puro: sem marcas de formatação, para ninguém mudar cor ou tamanho.
                var rotulo = new Label { enableRichText = false };
                rotulo.pickingMode = PickingMode.Ignore;
                rotulo.AddToClassList("chat-linha");
                rotulo.style.display = DisplayStyle.None;
                linhas.Add(rotulo);
                _rotulos.Add(rotulo);
            }

            _campo = new TextField { name = "social.chat.campo", maxLength = TamanhoMaximoDaCaixa };
            _campo.AddToClassList("chat-campo");
            _campo.style.display = DisplayStyle.None;
            _campo.RegisterCallback<KeyDownEvent>(AoApertarTecla, TrickleDown.TrickleDown);

            _raiz.Add(linhas);
            _raiz.Add(_campo);
            regiao.Add(_raiz);
        }

        /// <summary>Enter apertado com a caixa aberta.</summary>
        public event Action EnterApertado;

        /// <summary>Esc apertado com a caixa aberta.</summary>
        public event Action EscApertado;

        public bool Aberta => _campo.style.display == DisplayStyle.Flex;

        public string Texto
        {
            get => _campo.value ?? "";
            set => _campo.SetValueWithoutNotify(value ?? "");
        }

        public VisualElement Raiz => _raiz;

        public void Abrir()
        {
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
            _raiz.EnableInClassList("chat-esmaecido", esmaecido);
        }

        /// <summary>Mostra as linhas (as <see cref="LinhasVisiveis"/> últimas, da antiga para a nova).</summary>
        public void Mostrar(IReadOnlyList<LinhaDeChat> linhas, TimeSpan fuso)
        {
            var inicio = Math.Max(0, linhas.Count - LinhasVisiveis);
            for (var i = 0; i < _rotulos.Count; i++)
            {
                var rotulo = _rotulos[i];
                var indice = inicio + i;
                if (indice >= linhas.Count)
                {
                    rotulo.style.display = DisplayStyle.None;
                    continue;
                }

                var linha = linhas[indice];
                rotulo.style.display = DisplayStyle.Flex;
                rotulo.text = TextosDeChat.Linha(linha, fuso);
                rotulo.EnableInClassList("chat-cidade", linha.Canal == CanalDaLinha.Cidade);
                rotulo.EnableInClassList("chat-grupo", linha.Canal == CanalDaLinha.Grupo);
                rotulo.EnableInClassList("chat-sussurro", linha.Canal == CanalDaLinha.Sussurro);
                rotulo.EnableInClassList("chat-sistema", linha.Canal == CanalDaLinha.Sistema);
            }
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
                    EscApertado?.Invoke();
                    break;
            }
        }
    }
}
