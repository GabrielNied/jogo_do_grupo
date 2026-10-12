using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

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
            raiz.style.unityFontDefinition = FontDefinition.FromSDFFont(fonte);
            raiz.RegisterCallback<GeometryChangedEvent>(e =>
                AtualizarResponsividade(raiz, e.newRect.width)
            );
        }

        /// <summary>Font Asset SDF de um papel de <see cref="ConfigVisual.Fontes"/>.</summary>
        public static FontAsset CarregarFonte(string recurso)
        {
            return Resources.Load<FontAsset>(recurso)
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
                        e.ClassListContains("ui-titulo") || e.ClassListContains("titulo")
                            ? Mathf.Max(ConfigVisual.Tipografia.Titulo, leitura)
                        : e.ClassListContains("ui-subtitulo") || e.ClassListContains("subtitulo")
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

        /// <summary>Cor de token para desenhos feitos com Painter2D.</summary>
        public static Color Cor(string token)
        {
            if (ColorUtility.TryParseHtmlString(token, out var cor))
                return cor;
            if (token != null && token.StartsWith("rgba(") && token.EndsWith(")"))
            {
                var canais = token.Substring(5, token.Length - 6).Split(',');
                if (
                    canais.Length == 4
                    && byte.TryParse(canais[0].Trim(), out var r)
                    && byte.TryParse(canais[1].Trim(), out var g)
                    && byte.TryParse(canais[2].Trim(), out var b)
                    && float.TryParse(
                        canais[3].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var a
                    )
                    && a >= 0
                    && a <= 1
                )
                    return new Color(r / 255f, g / 255f, b / 255f, a);
            }
            throw new ArgumentException("Token de cor inválido.", nameof(token));
        }
    }
}
