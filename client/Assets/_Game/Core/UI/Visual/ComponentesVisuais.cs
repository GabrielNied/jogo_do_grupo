using System;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Visual
{
    public static class ComponentesVisuais
    {
        public static ScrollView PainelRolavel(bool social = false)
        {
            var painel = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto,
            };
            painel.AddToClassList("ui-painel");
            painel.AddToClassList("ui-painel-rolavel");
            if (social)
                painel.AddToClassList("ui-painel-social");
            return painel;
        }

        public static VisualElement Marca()
        {
            var marca = new VisualElement
            {
                name = "lumora.marca",
                tooltip = "Lumora — A New Horizon",
                pickingMode = PickingMode.Ignore,
            };
            marca.AddToClassList("ui-marca");
            return marca;
        }

        public static Label Texto(string texto, bool titulo = false, bool subtitulo = false)
        {
            var elemento = new Label(texto) { enableRichText = false };
            elemento.AddToClassList(
                titulo ? "ui-titulo"
                : subtitulo ? "ui-subtitulo"
                : "ui-texto"
            );
            elemento.style.unityFontDefinition = FontDefinition.FromSDFFont(
                EstiloVisual.CarregarFonte(
                    titulo || subtitulo ? ConfigVisual.Fontes.Titulo : ConfigVisual.Fontes.Leitura
                )
            );
            return elemento;
        }

        public static Button Botao(string texto, Action acao, bool principal = false)
        {
            var elemento = new Button(acao) { text = texto };
            elemento.AddToClassList("ui-botao");
            if (principal)
                elemento.AddToClassList("ui-primario");
            elemento.style.unityFontDefinition = FontDefinition.FromSDFFont(
                EstiloVisual.CarregarFonte(ConfigVisual.Fontes.Controle)
            );
            return elemento;
        }

        public static VisualElement Painel()
        {
            var elemento = new VisualElement();
            elemento.AddToClassList("ui-painel");
            return elemento;
        }
    }
}
