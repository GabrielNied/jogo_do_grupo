using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>Gera a imagem do terreno real em lotes e reutiliza a textura nas próximas aberturas.</summary>
    internal static class TexturaDoMapa
    {
        private static Color32[] _pixels;
        private static int _proximaLinha;
        private static Texture2D _textura;

        internal static int Geracoes { get; private set; }

        internal static Texture2D Pronta => _textura;

        internal static bool Avancar(int linhas)
        {
            if (_textura != null)
            {
                return true;
            }

            var lado = ProjecaoDoMapa.LadoDasAmostras;
            _pixels ??= new Color32[lado * lado];
            var ultima = Mathf.Min(lado, _proximaLinha + Mathf.Max(1, linhas));
            var passo = MapaDoMundo.Lado / lado;
            for (var linha = _proximaLinha; linha < ultima; linha++)
            {
                for (var coluna = 0; coluna < lado; coluna++)
                {
                    ProjecaoDoMapa.MundoDaAmostra(coluna, linha, out var x, out var z);
                    var altura = MapaDoMundo.AlturaEm(x, z);
                    var oeste = MapaDoMundo.AlturaEm(x - passo, z);
                    var norte = MapaDoMundo.AlturaEm(x, z + passo);
                    var declive =
                        Mathf.Max(Mathf.Abs(altura - oeste), Mathf.Abs(norte - altura)) / passo;
                    var cor = CoresDosBiomas.DoChao(
                        MapaDoMundo.PesosEm(x, z),
                        altura,
                        declive,
                        x,
                        z
                    );
                    var luz = Mathf.Clamp(
                        0.88f + (oeste - altura + norte - altura) * 0.025f,
                        0.6f,
                        1.1f
                    );
                    _pixels[linha * lado + coluna] = new Color(
                        cor.r * luz,
                        cor.g * luz,
                        cor.b * luz,
                        1f
                    );
                }
            }

            _proximaLinha = ultima;
            if (_proximaLinha < lado)
            {
                return false;
            }

            _textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = "Mapa do mundo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            _textura.SetPixels32(_pixels);
            _textura.Apply(false, true);
            Geracoes++;
            return true;
        }
    }
}
