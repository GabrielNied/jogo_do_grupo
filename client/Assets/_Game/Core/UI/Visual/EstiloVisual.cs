using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Visual
{
    public static class EstiloVisual
    {
        public static void Aplicar(VisualElement raiz)
        {
            if (raiz == null)
                throw new ArgumentNullException(nameof(raiz));
            var tokens = Resources.Load<StyleSheet>("Lumora/TokensVisuais");
            var componentes = Resources.Load<StyleSheet>("Lumora/ComponentesVisuais");
            var fonte = CarregarFonte(ConfigVisual.Fontes.Leitura);
            if (tokens == null || componentes == null)
                throw new InvalidOperationException(
                    "Estilos compartilhados de Lumora não importados."
                );
            if (raiz.ClassListContains("lumora-ui"))
                return;
            raiz.AddToClassList("lumora-ui");
            raiz.styleSheets.Add(tokens);
            raiz.styleSheets.Add(componentes);
            raiz.style.unityFontDefinition = FontDefinition.FromFont(fonte);
            raiz.RegisterCallback<GeometryChangedEvent>(e =>
                AtualizarResponsividade(raiz, e.newRect.width)
            );
        }

        public static Font CarregarFonte(string recurso)
        {
            return Resources.Load<Font>(recurso)
                ?? throw new InvalidOperationException("Fonte aprovada não importada: " + recurso);
        }

        public static void AtualizarResponsividade(VisualElement raiz, float largura)
        {
            if (raiz == null)
                throw new ArgumentNullException(nameof(raiz));
            if (largura <= 0 || float.IsNaN(largura) || float.IsInfinity(largura))
                return;
            raiz.EnableInClassList(
                "lumora-compacto",
                largura <= ConfigVisual.Responsividade.CompactoAte
            );
            raiz.EnableInClassList(
                "lumora-estreito",
                largura <= ConfigVisual.Responsividade.EstreitoAte
            );
        }

        public static void DefinirTamanhoTexto(VisualElement raiz, int tamanho)
        {
            if (raiz == null)
                throw new ArgumentNullException(nameof(raiz));
            var leitura = LimitarTexto(tamanho);
            raiz.style.fontSize = leitura;
            raiz.Query<TextElement>()
                .ForEach(e =>
                {
                    e.style.fontSize =
                        e.ClassListContains("ui-titulo")
                            ? Mathf.Max(ConfigVisual.Tipografia.Titulo, leitura)
                        : e.ClassListContains("ui-subtitulo")
                            ? Mathf.Max(ConfigVisual.Tipografia.Subtitulo, leitura)
                        : e is Button ? Mathf.Max(ConfigVisual.Tipografia.Controle, leitura)
                        : leitura;
                });
        }

        public static int LimitarTexto(int tamanho)
        {
            return Mathf.Clamp(
                tamanho,
                ConfigVisual.Tipografia.Leitura,
                ConfigVisual.Tipografia.LeituraMaxima
            );
        }
    }
}
