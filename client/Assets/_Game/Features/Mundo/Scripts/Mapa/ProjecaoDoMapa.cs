using System;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Um ponto que o mapa pode desenhar. A área vem da presença; hoje só existe a cidade.
    /// </summary>
    public readonly struct MarcadorDoMapa
    {
        public MarcadorDoMapa(float x, float z, float rotacaoY, string area, bool eu)
        {
            X = x;
            Z = z;
            RotacaoY = rotacaoY;
            Area = area;
            Eu = eu;
        }

        public float X { get; }
        public float Z { get; }
        public float RotacaoY { get; }
        public string Area { get; }
        public bool Eu { get; }
    }

    /// <summary>
    /// Converte posição do mundo em posição no minimapa e no mapa. Norte é +Z e fica para
    /// cima; o minimapa não gira com a câmera. Não desenha e não pede nada ao servidor.
    /// </summary>
    public static class ProjecaoDoMapa
    {
        /// <summary>Raio do minimapa, em metros. O canto da tela mostra este disco.</summary>
        public const float RaioDoMinimapa = 40f;

        /// <summary>A única área da fatia. Igual a <c>AreasDoMundo.Cidade</c> no servidor.</summary>
        public const string AreaDaCidade = "cidade";

        /// <summary>Quantas amostras de bioma o mapa-múndi pinta de cada lado.</summary>
        public const int LadoDasAmostras = 256;

        /// <summary>Pixel do centro de um lugar no mapa de tamanho informado (origem no topo).</summary>
        public static void PixelDoLugar(
            LugarDoMapa lugar,
            int largura,
            int altura,
            out int pixelX,
            out int pixelY
        )
        {
            if (lugar == null || lugar.Pecas == null || lugar.Pecas.Length == 0)
            {
                pixelX = largura / 2;
                pixelY = altura / 2;
                return;
            }

            var x = 0f;
            var z = 0f;
            foreach (var peca in lugar.Pecas)
            {
                x += peca.X;
                z += peca.Z;
            }

            NoMapa(x / lugar.Pecas.Length, z / lugar.Pecas.Length, out var u, out var v);
            pixelX = (int)Math.Round(u * largura);
            pixelY = (int)Math.Round((1f - v) * altura);
        }

        /// <summary>
        /// Ponto do minimapa em -1..1, com o jogador no centro e o norte para cima.
        /// Falso fora do disco de <see cref="RaioDoMinimapa"/>.
        /// </summary>
        public static bool NoMinimapa(float deltaX, float deltaZ, out float x, out float y)
        {
            x = deltaX / RaioDoMinimapa;
            y = deltaZ / RaioDoMinimapa;
            if (x * x + y * y > 1f)
            {
                x = 0f;
                y = 0f;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Giro da seta, em graus. Zero olha para o norte. O valor é o <c>RotacaoY</c> do
        /// personagem: a câmera de cima não gira, então a seta gira com ele.
        /// </summary>
        public static float AnguloDaSeta(float rotacaoY) => rotacaoY;

        /// <summary>Posição no mapa-múndi, de 0 a 1. (0, 0) é o canto sudoeste do mundo.</summary>
        public static void NoMapa(float mundoX, float mundoZ, out float u, out float v)
        {
            u = (mundoX + MapaDoMundo.Metade) / MapaDoMundo.Lado;
            v = (mundoZ + MapaDoMundo.Metade) / MapaDoMundo.Lado;
        }

        /// <summary>Inverso de <see cref="NoMapa"/>: do ponto 0..1 do mapa ao mundo, em metros.</summary>
        public static void DoMapa(float u, float v, out float mundoX, out float mundoZ)
        {
            mundoX = u * MapaDoMundo.Lado - MapaDoMundo.Metade;
            mundoZ = v * MapaDoMundo.Lado - MapaDoMundo.Metade;
        }

        /// <summary>Texto das coordenadas, em metros inteiros. X é leste e Z é norte.</summary>
        public static string Coordenadas(float x, float z) =>
            "X " + Math.Round(x).ToString("0") + "  Z " + Math.Round(z).ToString("0");

        /// <summary>Marcador de outra área não entra no mapa da área atual.</summary>
        public static bool NaAreaAtual(string areaDoMarcador, string areaAtual) =>
            areaDoMarcador == areaAtual;

        /// <summary>A cidade murada, em coordenadas 0..1 do mapa.</summary>
        public static void RetanguloDaCidade(out float u0, out float v0, out float u1, out float v1)
        {
            NoMapa(-MapaDoMundo.MetadeDaCidade, -MapaDoMundo.MetadeDaCidade, out u0, out v0);
            NoMapa(MapaDoMundo.MetadeDaCidade, MapaDoMundo.MetadeDaCidade, out u1, out v1);
        }

        /// <summary>Se o ponto está dentro dos muros. A área de presença continua sendo a cidade inteira.</summary>
        public static bool DentroDaCidade(float x, float z) =>
            Math.Abs(x) <= MapaDoMundo.MetadeDaCidade && Math.Abs(z) <= MapaDoMundo.MetadeDaCidade;

        /// <summary>Dentro do túnel da caverna: o minimapa não mostra o chão de cima.</summary>
        public static bool SobOChao(float x, float y, float z) =>
            PrimeiraCaverna.DentroDoTunel(x, z, y);

        /// <summary>Centro da amostra (coluna, linha), em metros. Linha 0 é o sul.</summary>
        public static void MundoDaAmostra(int coluna, int linha, out float x, out float z)
        {
            var passo = MapaDoMundo.Lado / LadoDasAmostras;
            x = -MapaDoMundo.Metade + (coluna + 0.5f) * passo;
            z = -MapaDoMundo.Metade + (linha + 0.5f) * passo;
        }
    }
}
