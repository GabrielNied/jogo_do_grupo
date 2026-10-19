using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>Tipos de vegetacao das clareiras, mata e colinas proximas da cidade.</summary>
    public enum AreaDoTerreno
    {
        /// <summary>Clareiras abertas entre os bosques.</summary>
        Campo,

        /// <summary>Bosques em volta da cidade, com arvores proximas.</summary>
        Mata,

        /// <summary>Dorsais locais entre a mata e as regioes externas.</summary>
        Colinas,
    }

    /// <summary>Uma peça do hub: qual prefab de <c>Resources/Mundo/</c> e onde fica.</summary>
    public readonly struct PecaDoHub
    {
        public PecaDoHub(string prefab, float x, float z, float guinada)
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
    }

    /// <summary>
    /// Medidas e disposição do hub (a cidade murada) e do terreno em volta. Dados puros (sem
    /// criar objetos): o <see cref="ConstrutorHub"/> instancia o que está aqui. A grade e o
    /// tamanho estão documentados em <c>docs/features/mundo.md</c>.
    /// </summary>
    public static class LayoutDoHub
    {
        /// <summary>Lado do hub, em metros. O hub é um quadrado centrado na origem.</summary>
        public const float Lado = 80f;

        /// <summary>Medida da grade de encaixe das peças, em metros (peça Kenney x 4).</summary>
        public const float Modulo = 4f;

        /// <summary>Metade do lado: o chão vai de -Metade a +Metade em X e em Z.</summary>
        public const float Metade = Lado / 2f;

        /// <summary>Espessura do muro, igual a uma célula da grade.</summary>
        public const float EspessuraMuro = Modulo;

        /// <summary>Altura do muro, em metros.</summary>
        public const float AlturaMuro = 6f;

        /// <summary>Altura das torres de canto, em metros.</summary>
        public const float AlturaTorre = 12f;

        /// <summary>Metade da área andável (face interna dos muros), em metros.</summary>
        public const float MetadeAndavel = Metade - EspessuraMuro;

        /// <summary>Número de células por lado da grade (20).</summary>
        public const int CelulasPorLado = (int)(Lado / Modulo);

        /// <summary>Metade da praça de pedra no centro, em metros (praça de 24 x 24 m).</summary>
        public const float MetadePraca = 12f;

        /// <summary>Metade da largura das duas estradas em cruz, em metros (8 m de largura).</summary>
        public const float MetadeEstrada = 4f;

        /// <summary>
        /// Centro da rua em anel, em metros: um quadrado de uma célula (4 m) de largura que passa
        /// por <c>x</c> ou <c>z</c> = ±22 e liga as duas estradas em cruz.
        /// </summary>
        public const float CentroDoAnel = 22f;

        /// <summary>Ruas laterais que ligam a praça ao anel (uma célula de largura).</summary>
        public const float CentroRuaLateral = 14f;

        /// <summary>Lado do terreno em volta da cidade, em metros (cidade no meio, cerca de 1 km).</summary>
        public const float LadoDoTerreno = 1000f;

        /// <summary>Metade do terreno: ele vai de -Metade a +Metade em X e em Z.</summary>
        public const float MetadeDoTerreno = LadoDoTerreno / 2f;

        /// <summary>
        /// Metade da largura do portão norte, em metros: o vão no muro tem 8 m (duas células), na
        /// linha de células do norte, e continua na estrada que sai da cidade.
        /// </summary>
        public const float MetadeDoPortao = 4f;

        /// <summary>Comprimento da estrada que sai do portão, medido a partir do muro, em metros.</summary>
        public const float ComprimentoDaEstradaFora = 40f;

        public const string PrefabChao = "Chao_Hub";
        public const string PrefabTerreno = "Terreno_Fora";
        public const string PrefabMuro = "Muro_Bloco";
        public const string PrefabTorre = "Torre_Canto";
        public const string PrefabEstrada = "Estrada";

        /// <summary>Onde o jogador nasce: pés no chão, ao sul da fonte, olhando para ela (+Z).</summary>
        public static Vector3 PontoDeNascimento
        {
            get
            {
                var ponto = NascimentoDoMapa.Atual;
                return new Vector3(ponto.X, ponto.Y.GetValueOrDefault(), ponto.Z);
            }
        }

        /// <summary>Para onde o jogador olha ao nascer (+Z).</summary>
        public static readonly Quaternion RotacaoDeNascimento = Quaternion.identity;

        /// <summary>
        /// Props da cidade (fonte, bancos, lanternas, casas, barracas, árvores e pedras), todos
        /// dentro da área andável e fora das estradas e da rua em anel (as lanternas ficam na beira
        /// delas).
        /// </summary>
        public static readonly IReadOnlyList<PecaDoHub> Props = MontarProps();

        /// <summary>
        /// Árvores e pedras do terreno de fora, espalhadas por uma semente fixa (sempre o mesmo
        /// resultado): longe da cidade, fora do corredor da estrada do portão, e com densidade
        /// diferente em cada <see cref="AreaDoTerreno"/>.
        /// </summary>
        public static readonly IReadOnlyList<PecaDoHub> PropsFora = GerarPropsFora();

        /// <summary>
        /// Tipo de vegetacao local, seguindo o mapa novo do shared em vez de setores fixos.
        /// </summary>
        public static AreaDoTerreno AreaEm(float x, float z)
        {
            var bioma = MapaDoMundo.BiomaEm(x, z);
            if (bioma == Bioma.Campo)
            {
                return AreaDoTerreno.Campo;
            }

            if (bioma == Bioma.Colinas)
            {
                return AreaDoTerreno.Colinas;
            }

            return AreaDoTerreno.Mata;
        }

        /// <summary>
        /// Centro da célula <paramref name="indice"/> (0 a <see cref="CelulasPorLado"/> - 1) ao
        /// longo de um eixo, em metros.
        /// </summary>
        public static float CentroDaCelula(int indice)
        {
            return -Metade + Modulo * (indice + 0.5f);
        }

        /// <summary>
        /// Se o ponto (no plano XZ) fica na área andável (dentro dos muros), com
        /// <paramref name="margem"/> de folga em relação a eles.
        /// </summary>
        public static bool DentroDaAreaAndavel(Vector3 ponto, float margem)
        {
            var limite = MetadeAndavel - margem;
            return Mathf.Abs(ponto.x) <= limite && Mathf.Abs(ponto.z) <= limite;
        }

        /// <summary>Se a coluna de células centrada em <paramref name="x"/> é o vão do portão.</summary>
        public static bool EhVaoDoPortao(float x)
        {
            return Mathf.Abs(x) < MetadeDoPortao;
        }

        /// <summary>
        /// Se a coluna de células centrada em <paramref name="x"/> é uma das duas torres que
        /// ladeiam o portão (as células vizinhas ao vão).
        /// </summary>
        public static bool EhTorreDoPortao(float x)
        {
            return Mathf.Abs(Mathf.Abs(x) - (MetadeDoPortao + Modulo / 2f)) < 0.01f;
        }

        /// <summary>
        /// Se a célula (<paramref name="x"/>, <paramref name="z"/>) da cidade é estrada de pedra:
        /// a praça, as duas estradas em cruz ou a rua em anel.
        /// </summary>
        public static bool EhEstradaDaCidade(float x, float z)
        {
            var ax = Mathf.Abs(x);
            var az = Mathf.Abs(z);

            var naPraca = ax < MetadePraca && az < MetadePraca;
            var naCruz = ax < MetadeEstrada || az < MetadeEstrada;
            var noAnel = Mathf.Abs(Mathf.Max(ax, az) - CentroDoAnel) < Modulo / 4f;
            var faixa = Modulo / 4f;
            var ruaLateralX =
                Mathf.Abs(ax - CentroRuaLateral) < faixa && az > MetadePraca && az < CentroDoAnel;
            var ruaLateralMenosX =
                Mathf.Abs(ax + CentroRuaLateral) < faixa && az > MetadePraca && az < CentroDoAnel;
            var ruaLateralZ =
                Mathf.Abs(az - CentroRuaLateral) < faixa && ax > MetadePraca && ax < CentroDoAnel;
            var ruaLateralMenosZ =
                Mathf.Abs(az + CentroRuaLateral) < faixa && ax > MetadePraca && ax < CentroDoAnel;
            var mercado =
                x > MetadePraca + 2f && x < CentroDoAnel - 2f && z > 2f && z < MetadePraca + 4f;
            var mercadoEspelho =
                x < -(MetadePraca + 2f)
                && x > -(CentroDoAnel - 2f)
                && z > 2f
                && z < MetadePraca + 4f;
            return naPraca
                || naCruz
                || noAnel
                || ruaLateralX
                || ruaLateralMenosX
                || ruaLateralZ
                || ruaLateralMenosZ
                || mercado
                || mercadoEspelho;
        }

        private static IReadOnlyList<PecaDoHub> MontarProps()
        {
            var lista = new List<PecaDoHub>
            {
                new PecaDoHub("Fonte", 0f, 0f, 0f),
                // Bancos em volta da fonte.
                new PecaDoHub("Banco", -7f, -7f, 45f),
                new PecaDoHub("Banco", 7f, -7f, -45f),
                new PecaDoHub("Banco", -7f, 7f, -45f),
                new PecaDoHub("Banco", 7f, 7f, 45f),
                // Lanternas nos cantos da praça.
                new PecaDoHub("Lanterna", -11f, -11f, 0f),
                new PecaDoHub("Lanterna", 11f, -11f, 0f),
                new PecaDoHub("Lanterna", -11f, 11f, 0f),
                new PecaDoHub("Lanterna", 11f, 11f, 0f),
                // Lanternas do portão (as torres ficam em x = ±6, z = 38).
                new PecaDoHub("Lanterna", -5.2f, 33.8f, 0f),
                new PecaDoHub("Lanterna", 5.2f, 33.8f, 0f),
            };

            // Um quadrante (x e z positivos) é desenhado e espelhado para os outros três.
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    var pedra = sx * sz > 0;
                    var casaPequena = pedra ? "Casa_Pedra_Pequena" : "Casa_Madeira_Pequena";
                    var casaLarga = pedra ? "Casa_Pedra_Larga" : "Casa_Madeira_Larga";

                    // Lanternas na esquina da rua em anel com cada estrada em cruz.
                    lista.Add(new PecaDoHub("Lanterna", sx * 5.2f, sz * 20.6f, 0f));

                    // Três casas pequenas em volta da praça, de porta para ela (as duas primeiras)
                    // e para a rua em anel (a terceira).
                    AdicionarCasa(lista, casaPequena, sx, sz, 8f, 16f, 0f, -1f);
                    AdicionarCasa(lista, casaPequena, sx, sz, 16f, 8f, -1f, 0f);
                    AdicionarCasa(lista, casaPequena, sx, sz, 16f, 16f, 1f, 0f);

                    // Duas casas largas do outro lado da rua em anel, de porta para ela.
                    AdicionarCasa(lista, casaLarga, sx, sz, 28f, 12f, -1f, 0f);
                    AdicionarCasa(lista, casaLarga, sx, sz, 12f, 28f, 0f, -1f);

                    // Sem quarteirão extra nem barracas no mercado: as casas (8 x 8 m e 12 x 8 m)
                    // e as barracas que a #274 pôs ali ficavam em cima da rua em anel e das casas
                    // vizinhas. O chão de pedra do mercado e das ruas laterais continua.

                    // Praça do canto: barraca virada para a fonte, árvores e uma carroça ou pedra.
                    var barraca = pedra ? "Barraca_Vermelha" : "Barraca_Verde";
                    var guinada = sx < 0 ? (sz < 0 ? 45f : 135f) : (sz < 0 ? -45f : -135f);
                    lista.Add(new PecaDoHub(barraca, sx * 28.8f, sz * 27.2f, guinada));
                    lista.Add(new PecaDoHub("Arvore_Alta", sx * 33.2f, sz * 33.2f, 40f * (sx + 2)));
                    lista.Add(new PecaDoHub("Arvore", sx * 32.8f, sz * 26.2f, 30f * (sz + 3)));
                    lista.Add(
                        pedra
                            ? new PecaDoHub("Pedra_Pequena", sx * 26.6f, sz * 33.6f, 0f)
                            : new PecaDoHub("Carroca", sx * 27f, sz * 33f, 0f)
                    );

                    // Árvores no que sobra ao lado das casas largas.
                    lista.Add(new PecaDoHub("Arvore", sx * 33.8f, sz * 9f, 70f));
                    lista.Add(new PecaDoHub("Arvore", sx * 33.8f, sz * 15f, 110f));
                    lista.Add(new PecaDoHub("Arvore", sx * 9f, sz * 33.8f, 160f));
                    lista.Add(new PecaDoHub("Arvore", sx * 15f, sz * 33.8f, 200f));
                }
            }

            return lista;
        }

        // A porta do prefab da casa fica no lado -Z; a guinada gira a casa para a frente pedida
        // (frenteX, frenteZ), dada para o quadrante positivo e espelhada com os sinais.
        private static void AdicionarCasa(
            List<PecaDoHub> lista,
            string prefab,
            int sx,
            int sz,
            float x,
            float z,
            float frenteX,
            float frenteZ
        )
        {
            var guinada = Mathf.Atan2(-frenteX * sx, -frenteZ * sz) * Mathf.Rad2Deg;
            lista.Add(new PecaDoHub(prefab, sx * x, sz * z, guinada));
        }

        // Folga em volta do quadrado da cidade, da borda do mapa e do corredor da estrada.
        private const float FolgaDaCidade = 24f;
        private const float FolgaDaBorda = 12f;
        private const float MetadeDoCorredor = 6f;

        // Até esta distância do centro (Chebyshev) a vegetação é mais densa; depois, mais rala.
        private const float AlcancePerto = 220f;

        private static IReadOnlyList<PecaDoHub> GerarPropsFora()
        {
            var sorteio = new Sorteio(20261011u);
            var lista = new List<PecaDoHub>();
            var campo = new[]
            {
                "Arvore_Redonda",
                "Arvore",
                "Arvore",
                "Arvore_Alta",
                "Pedra_Pequena",
            };
            var mata = new[]
            {
                "Arvore",
                "Arvore",
                "Arvore_Alta",
                "Arvore_Alta",
                "Arvore_Torta",
                "Arvore_Redonda",
            };
            var colinas = new[]
            {
                "Pedra_Grande",
                "Pedra_Grande",
                "Pedra_Larga",
                "Pedra_Pequena",
                "Arvore_Torta",
                "Arvore",
            };

            // Campo: aberto, árvores afastadas. Mata: muitas árvores. Colinas: mais pedra.
            Espalhar(lista, ref sorteio, campo, 36, 18f, true, AreaDoTerreno.Campo);
            Espalhar(lista, ref sorteio, campo, 28, 28f, false, AreaDoTerreno.Campo);
            Espalhar(lista, ref sorteio, mata, 170, 8f, true, AreaDoTerreno.Mata);
            Espalhar(lista, ref sorteio, mata, 150, 12f, false, AreaDoTerreno.Mata);
            Espalhar(lista, ref sorteio, colinas, 48, 12f, true, AreaDoTerreno.Colinas);
            Espalhar(lista, ref sorteio, colinas, 36, 18f, false, AreaDoTerreno.Colinas);

            return lista;
        }

        private static void Espalhar(
            List<PecaDoHub> lista,
            ref Sorteio sorteio,
            string[] nomes,
            int quantidade,
            float distanciaMinima,
            bool pertoDaCidade,
            AreaDoTerreno area
        )
        {
            var colocadas = 0;
            var distancia2 = distanciaMinima * distanciaMinima;
            for (var tentativa = 0; tentativa < 16000 && colocadas < quantidade; tentativa++)
            {
                var x = (sorteio.Proximo() * 2f - 1f) * (MetadeDoTerreno - FolgaDaBorda);
                var z = (sorteio.Proximo() * 2f - 1f) * (MetadeDoTerreno - FolgaDaBorda);
                if (!CabeNaArea(x, z, area, pertoDaCidade))
                {
                    continue;
                }

                var perto = false;
                foreach (var outra in lista)
                {
                    var dx = outra.X - x;
                    var dz = outra.Z - z;
                    if (dx * dx + dz * dz < distancia2)
                    {
                        perto = true;
                        break;
                    }
                }

                if (perto)
                {
                    continue;
                }

                var prefab = nomes[(int)(sorteio.Proximo() * nomes.Length) % nomes.Length];
                lista.Add(new PecaDoHub(prefab, x, z, sorteio.Proximo() * 360f));
                colocadas++;
            }
        }

        private static bool CabeNaArea(float x, float z, AreaDoTerreno area, bool pertoDaCidade)
        {
            if (Mathf.Abs(x) > MetadeDoTerreno - FolgaDaBorda)
            {
                return false;
            }

            if (Mathf.Abs(z) > MetadeDoTerreno - FolgaDaBorda)
            {
                return false;
            }

            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < Metade + FolgaDaCidade)
            {
                return false;
            }

            // Corredor da estrada que sai do portão, até a borda norte.
            if (Mathf.Abs(x) < MetadeDoCorredor && z > 0f)
            {
                return false;
            }

            var bioma = MapaDoMundo.BiomaEm(x, z);
            if (bioma != Bioma.Campo && bioma != Bioma.Mata && bioma != Bioma.Colinas)
            {
                return false;
            }

            var areaDoBioma =
                bioma == Bioma.Campo ? AreaDoTerreno.Campo
                : bioma == Bioma.Colinas ? AreaDoTerreno.Colinas
                : AreaDoTerreno.Mata;
            if (areaDoBioma != area)
            {
                return false;
            }

            var alcance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (pertoDaCidade)
            {
                return alcance < AlcancePerto;
            }

            return alcance >= AlcancePerto;
        }

        // Gerador congruencial linear próprio: o resultado não depende da plataforma.
        private struct Sorteio
        {
            public uint Estado;

            public Sorteio(uint estado)
            {
                Estado = estado;
            }

            public float Proximo()
            {
                Estado = unchecked(Estado * 1664525u + 1013904223u);
                return (Estado >> 8) / 16777216f;
            }
        }
    }
}
