using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Os quatro lugares desta leva. Não é um gerador: cada um tem coordenada escrita.</summary>
    public enum TipoDeEstrutura : byte
    {
        /// <summary>Muros de pedra quebrados.</summary>
        Ruina = 0,

        /// <summary>Barraca, carroça e banco.</summary>
        Acampamento = 1,

        /// <summary>Lajes de pedra entre dois marcos, no chão andável.</summary>
        Passagem = 2,

        /// <summary>Pedra e lanterna à beira da estrada que sai da cidade pelo norte.</summary>
        Marco = 3,
    }

    /// <summary>
    /// Uma peça de um lugar. A base fica na altura do <see cref="MapaDoMundo"/> naquele ponto,
    /// para o colisor encostar no chão em vez de atravessá-lo.
    /// </summary>
    public readonly struct PecaDeEstrutura
    {
        public PecaDeEstrutura(string prefab, float x, float z, float guinada)
        {
            Prefab = prefab;
            X = x;
            Z = z;
            Guinada = guinada;
        }

        /// <summary>Nome do prefab em <c>Resources/Mundo/</c> (sem extensão).</summary>
        public string Prefab { get; }

        public float X { get; }

        public float Z { get; }

        /// <summary>Rotação em torno do eixo Y, em graus.</summary>
        public float Guinada { get; }

        /// <summary>Altura da base, em metros: o chão do mapa neste ponto.</summary>
        public float Altura => MapaDoMundo.AlturaEm(X, Z);
    }

    /// <summary>Um lugar fixo: nome, tipo, bioma em que deve cair e as peças que o formam.</summary>
    public sealed class LugarDoMapa
    {
        public LugarDoMapa(string nome, TipoDeEstrutura tipo, Bioma bioma, PecaDeEstrutura[] pecas)
        {
            Nome = nome;
            Tipo = tipo;
            Bioma = bioma;
            Pecas = pecas;
        }

        public string Nome { get; }

        public TipoDeEstrutura Tipo { get; }

        public Bioma Bioma { get; }

        public PecaDeEstrutura[] Pecas { get; }
    }

    /// <summary>
    /// Lugares feitos desta leva: ruína, acampamento, passagem e marco. Coordenadas escritas,
    /// sempre as mesmas. Peças do Kenney Fantasy Town Kit (CC0), já usadas no hub.
    /// </summary>
    public static class EstruturasDoMapa
    {
        /// <summary>
        /// Subida máxima em 1 m de chão que o personagem ainda sobe (45 graus, regra do Core).
        /// </summary>
        public const float InclinacaoMaxima = 1f;

        /// <summary>Distância, em metros, usada para medir a encosta em volta de uma peça.</summary>
        public const float PassoDaEncosta = 4f;

        public static readonly LugarDoMapa[] Lugares =
        {
            new LugarDoMapa(
                "Ruina do oeste",
                TipoDeEstrutura.Ruina,
                Bioma.Colinas,
                new[]
                {
                    new PecaDeEstrutura("Muro_Bloco", -250f, -100f, 15f),
                    new PecaDeEstrutura("Muro_Bloco", -246f, -101f, -10f),
                    new PecaDeEstrutura("Pedra_Grande", -248f, -104f, 0f),
                }
            ),
            new LugarDoMapa(
                "Acampamento da mata",
                TipoDeEstrutura.Acampamento,
                Bioma.Mata,
                new[]
                {
                    new PecaDeEstrutura("Barraca_Vermelha", 250f, -100f, 20f),
                    new PecaDeEstrutura("Carroca", 254f, -98f, 70f),
                    new PecaDeEstrutura("Banco", 247f, -98f, 110f),
                }
            ),
            new LugarDoMapa(
                "Passagem das colinas",
                TipoDeEstrutura.Passagem,
                Bioma.Colinas,
                new[]
                {
                    new PecaDeEstrutura("Estrada", -302f, -200f, 0f),
                    new PecaDeEstrutura("Estrada", -298f, -200f, 0f),
                    new PecaDeEstrutura("Estrada", -294f, -200f, 0f),
                    new PecaDeEstrutura("Pedra_Grande", -306f, -200f, 0f),
                    new PecaDeEstrutura("Pedra_Grande", -290f, -200f, 0f),
                }
            ),
            new LugarDoMapa(
                "Marco da estrada norte",
                TipoDeEstrutura.Marco,
                Bioma.Campo,
                new[]
                {
                    new PecaDeEstrutura("Pedra_Grande", 14f, 70f, 0f),
                    new PecaDeEstrutura("Lanterna", 14f, 74f, 0f),
                }
            ),
        };

        /// <summary>Fora dos muros: a cidade é o quadrado de <see cref="MapaDoMundo.MetadeDaCidade"/>.</summary>
        public static bool ForaDaCidade(float x, float z) =>
            Math.Abs(x) > MapaDoMundo.MetadeDaCidade || Math.Abs(z) > MapaDoMundo.MetadeDaCidade;

        /// <summary>Chão acima da água e da lava.</summary>
        public static bool ChaoSeco(float x, float z)
        {
            var altura = MapaDoMundo.AlturaEm(x, z);
            return altura > MapaDoMundo.NivelDaAgua && altura > MapaDoMundo.NivelDaLava;
        }

        /// <summary>A encosta em volta do ponto ainda é a que o personagem sobe.</summary>
        public static bool EncostaAndavel(float x, float z)
        {
            var altura = MapaDoMundo.AlturaEm(x, z);
            var passo = PassoDaEncosta;
            return Subida(altura, x + passo, z) <= InclinacaoMaxima
                && Subida(altura, x - passo, z) <= InclinacaoMaxima
                && Subida(altura, x, z + passo) <= InclinacaoMaxima
                && Subida(altura, x, z - passo) <= InclinacaoMaxima;
        }

        private static float Subida(float altura, float x, float z) =>
            Math.Abs(MapaDoMundo.AlturaEm(x, z) - altura) / PassoDaEncosta;
    }
}
