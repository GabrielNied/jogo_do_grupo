using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Dica flutuante da interface (o <c>tooltip</c> do UI Toolkit só existe no editor): uma
    /// caixa escura que segue o mouse sobre slots, ícones e botões. Fica no documento do Core,
    /// por cima de tudo. Os painéis das funcionalidades usam a mesma escala, então a posição do
    /// ponteiro vale nos dois.
    /// </summary>
    public static class DicaHud
    {
        private const float Distancia = 18f;

        private static VisualElement _caixa;
        private static Label _titulo;
        private static Label _subtitulo;
        private static Label _texto;
        private static Label _rodape;
        private static VisualElement _alvoAtual;
        private static string _classeTitulo;

        /// <summary>Liga a dica ao elemento: aparece ao entrar com o mouse e some ao sair.</summary>
        public static void Registrar(VisualElement alvo, Func<ConteudoDaDica> conteudo)
        {
            if (alvo == null || conteudo == null)
                return;
            if (alvo.pickingMode == PickingMode.Ignore)
                alvo.pickingMode = PickingMode.Position;
            alvo.RegisterCallback<PointerEnterEvent>(e =>
            {
                _alvoAtual = alvo;
                Mostrar(conteudo(), e.position);
            });
            alvo.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_alvoAtual == alvo)
                    Posicionar(e.position);
            });
            alvo.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (_alvoAtual == alvo)
                    Esconder();
            });
            alvo.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (_alvoAtual == alvo)
                    Esconder();
            });
        }

        /// <summary>Esconde a dica (ex.: ao fechar a janela que a mostrava).</summary>
        public static void Esconder()
        {
            _alvoAtual = null;
            if (_caixa != null)
                _caixa.style.display = DisplayStyle.None;
        }

        private static void Mostrar(ConteudoDaDica conteudo, Vector2 posicao)
        {
            if (conteudo == null || !GarantirCaixa())
                return;
            Preencher(_titulo, conteudo.Titulo);
            Preencher(_subtitulo, conteudo.Subtitulo);
            Preencher(_texto, conteudo.Texto);
            Preencher(_rodape, conteudo.Rodape);
            if (_classeTitulo != null)
                _titulo.RemoveFromClassList(_classeTitulo);
            _classeTitulo = conteudo.ClasseTitulo;
            if (_classeTitulo != null)
                _titulo.AddToClassList(_classeTitulo);
            _caixa.style.display = DisplayStyle.Flex;
            _caixa.BringToFront();
            Posicionar(posicao);
        }

        private static void Posicionar(Vector2 posicao)
        {
            if (_caixa == null || _caixa.parent == null)
                return;
            var area = _caixa.parent.contentRect;
            var largura = _caixa.resolvedStyle.width;
            var altura = _caixa.resolvedStyle.height;
            if (float.IsNaN(largura))
                largura = 0f;
            if (float.IsNaN(altura))
                altura = 0f;
            // À direita e acima do ponteiro; vira para o outro lado perto das bordas.
            var x = posicao.x + Distancia;
            var y = posicao.y - altura - Distancia;
            if (x + largura > area.width)
                x = posicao.x - largura - Distancia;
            if (y < 0f)
                y = posicao.y + Distancia;
            _caixa.style.left = Mathf.Max(0f, x);
            _caixa.style.top = Mathf.Max(0f, y);
        }

        private static void Preencher(Label rotulo, string texto)
        {
            rotulo.text = texto ?? string.Empty;
            rotulo.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private static bool GarantirCaixa()
        {
            var container = Navegacao.Container;
            if (container == null)
                return false;
            if (_caixa != null && _caixa.panel != null && _caixa.parent == container)
                return true;

            _caixa = new VisualElement { name = "core.dica", pickingMode = PickingMode.Ignore };
            _caixa.AddToClassList("hud-dica");
            _titulo = ComponentesHud.Texto(string.Empty, "hud-dica-titulo");
            _subtitulo = ComponentesHud.Texto(string.Empty, "hud-dica-subtitulo");
            _texto = ComponentesHud.Texto(string.Empty, "hud-dica-texto");
            _rodape = ComponentesHud.Texto(string.Empty, "hud-dica-rodape");
            _caixa.Add(_titulo);
            _caixa.Add(_subtitulo);
            _caixa.Add(_texto);
            _caixa.Add(_rodape);
            _caixa.style.display = DisplayStyle.None;
            container.Add(_caixa);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _caixa = null;
            _alvoAtual = null;
            _classeTitulo = null;
        }
    }
}
