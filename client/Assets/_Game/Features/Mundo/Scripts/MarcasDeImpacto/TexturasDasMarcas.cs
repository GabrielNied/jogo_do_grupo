using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto
{
    /// <summary>
    /// Textura e malha das marcas de impacto, geradas por código (sem arquivo de terceiros): um
    /// borrão irregular escuro; a martelada tem rachaduras radiais e a fuligem é mais difusa.
    /// Criadas uma vez e compartilhadas por todas as marcas.
    /// </summary>
    internal static class TexturasDasMarcas
    {
        private const int Tamanho = 128;

        private static Texture2D _martelada;
        private static Texture2D _fuligem;
        private static Mesh _quadrado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _martelada = null;
            _fuligem = null;
            _quadrado = null;
        }

        public static Texture2D Obter(TipoDeMarca tipo)
        {
            if (tipo == TipoDeMarca.Martelada)
            {
                return _martelada != null ? _martelada : (_martelada = Gerar(true));
            }

            return _fuligem != null ? _fuligem : (_fuligem = Gerar(false));
        }

        /// <summary>Quadrado de 1 m no plano XZ, virado para cima (a escala dá o diâmetro).</summary>
        public static Mesh Quadrado()
        {
            if (_quadrado != null)
            {
                return _quadrado;
            }

            _quadrado = new Mesh { name = "Marca de impacto", hideFlags = HideFlags.DontSave };
            _quadrado.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f),
            };
            _quadrado.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            _quadrado.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quadrado.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            _quadrado.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
            return _quadrado;
        }

        private static Texture2D Gerar(bool rachaduras)
        {
            var textura = new Texture2D(Tamanho, Tamanho, TextureFormat.RGBA32, false)
            {
                name = rachaduras ? "Marca martelada" : "Marca fuligem",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var semente = rachaduras ? 11.3f : 47.9f;
            var cor = rachaduras ? new Color(0.16f, 0.12f, 0.08f) : new Color(0.05f, 0.05f, 0.05f);
            var pixels = new Color[Tamanho * Tamanho];
            for (var y = 0; y < Tamanho; y++)
            {
                for (var x = 0; x < Tamanho; x++)
                {
                    var u = x / (Tamanho - 1f) * 2f - 1f;
                    var v = y / (Tamanho - 1f) * 2f - 1f;
                    var raio = Mathf.Sqrt(u * u + v * v);
                    var angulo = Mathf.Atan2(v, u);
                    // Borda irregular: o raio útil oscila com o ângulo.
                    var borda =
                        0.78f
                        + 0.14f
                            * Mathf.PerlinNoise(
                                semente + Mathf.Cos(angulo) * 1.7f,
                                semente + Mathf.Sin(angulo) * 1.7f
                            )
                        - 0.07f;
                    var alfa = Mathf.Clamp01((borda - raio) / 0.35f);
                    if (rachaduras)
                    {
                        // Rachaduras radiais: faixas escuras estreitas que saem do centro.
                        var faixa = Mathf.PerlinNoise(semente + angulo * 3.1f, 0.5f);
                        var fenda = Mathf.Clamp01(1f - Mathf.Abs(faixa - 0.5f) * 9f);
                        alfa = Mathf.Clamp01(alfa * 0.55f + fenda * alfa * 0.6f);
                    }
                    else
                    {
                        var mancha = Mathf.PerlinNoise(semente + u * 4f, semente + v * 4f);
                        alfa *= 0.5f + 0.5f * mancha;
                    }

                    pixels[y * Tamanho + x] = new Color(cor.r, cor.g, cor.b, alfa);
                }
            }

            textura.SetPixels(pixels);
            textura.Apply(false, true);
            return textura;
        }
    }
}
