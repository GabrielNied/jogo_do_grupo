using System;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Visual
{
    public static class ComponentesVisuais
    {
        public static Label Texto(string texto, bool titulo = false, bool subtitulo = false)
        {
            var elemento = new Label(texto) { enableRichText = false };
            elemento.AddToClassList(
                titulo ? "ui-titulo"
                : subtitulo ? "ui-subtitulo"
                : "ui-texto"
            );
            elemento.style.unityFontDefinition = FontDefinition.FromFont(
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
            elemento.style.unityFontDefinition = FontDefinition.FromFont(
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
