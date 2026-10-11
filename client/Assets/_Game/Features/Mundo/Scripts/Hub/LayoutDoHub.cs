using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
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
        public const float Lado = 40f;

        /// <summary>Medida da grade de encaixe das peças, em metros (peça Kenney x 2).</summary>
        public const float Modulo = 2f;

        /// <summary>Metade do lado: o chão vai de -Metade a +Metade em X e em Z.</summary>
        public const float Metade = Lado / 2f;

        /// <summary>Espessura do muro, igual a uma célula da grade.</summary>
        public const float EspessuraMuro = Modulo;

        /// <summary>Altura do muro, em metros.</summary>
        public const float AlturaMuro = 3f;

        /// <summary>Altura das torres de canto, em metros.</summary>
        public const float AlturaTorre = 6f;

        /// <summary>Metade da área andável (face interna dos muros), em metros.</summary>
        public const float MetadeAndavel = Metade - EspessuraMuro;

        /// <summary>Número de células por lado da grade (20).</summary>
        public const int CelulasPorLado = (int)(Lado / Modulo);

        /// <summary>Metade da praça de pedra no centro, em metros (praça de 12 x 12 m).</summary>
        public const float MetadePraca = 6f;

        /// <summary>Metade da largura das duas estradas em cruz, em metros (4 m de largura).</summary>
        public const float MetadeEstrada = 2f;

        /// <summary>
        /// Centro da rua em anel, em metros: um quadrado de uma célula (2 m) de largura que passa
        /// por <c>x</c> ou <c>z</c> = ±11 e liga as duas estradas em cruz.
        /// </summary>
        public const float CentroDoAnel = 11f;

        /// <summary>Lado do terreno de grama em volta da cidade, em metros (cidade no meio).</summary>
        public const float LadoDoTerreno = 120f;

        /// <summary>Metade do terreno: ele vai de -Metade a +Metade em X e em Z.</summary>
        public const float MetadeDoTerreno = LadoDoTerreno / 2f;

        /// <summary>
        /// Metade da largura do portão norte, em metros: o vão no muro tem 4 m (duas células), na
        /// linha de células do norte, e continua na estrada que sai da cidade.
        /// </summary>
        public const float MetadeDoPortao = 2f;

        /// <summary>Comprimento da estrada que sai do portão, medido a partir do muro, em metros.</summary>
        public const float ComprimentoDaEstradaFora = 20f;

        public const string PrefabChao = "Chao_Hub";
        public const string PrefabTerreno = "Terreno_Fora";
        public const string PrefabMuro = "Muro_Bloco";
        public const string PrefabTorre = "Torre_Canto";
        public const string PrefabEstrada = "Estrada";

        /// <summary>Onde o jogador nasce: pés no chão, ao sul da fonte, olhando para ela (+Z).</summary>
        public static readonly Vector3 PontoDeNascimento = new Vector3(0f, 0f, -12f);

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
        /// resultado): longe da cidade e fora do corredor da estrada do portão.
        /// </summary>
        public static readonly IReadOnlyList<PecaDoHub> PropsFora = GerarPropsFora();

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
            return naPraca || naCruz || noAnel;
        }

        private static IReadOnlyList<PecaDoHub> MontarProps()
        {
            var lista = new List<PecaDoHub>
            {
                new PecaDoHub("Fonte", 0f, 0f, 0f),
                // Bancos em volta da fonte.
                new PecaDoHub("Banco", -3.5f, -3.5f, 45f),
                new PecaDoHub("Banco", 3.5f, -3.5f, -45f),
                new PecaDoHub("Banco", -3.5f, 3.5f, -45f),
                new PecaDoHub("Banco", 3.5f, 3.5f, 45f),
                // Lanternas nos cantos da praça.
                new PecaDoHub("Lanterna", -5.5f, -5.5f, 0f),
                new PecaDoHub("Lanterna", 5.5f, -5.5f, 0f),
                new PecaDoHub("Lanterna", -5.5f, 5.5f, 0f),
                new PecaDoHub("Lanterna", 5.5f, 5.5f, 0f),
                // Lanternas do portão (as torres ficam em x = ±3, z = 19).
                new PecaDoHub("Lanterna", -2.6f, 16.9f, 0f),
                new PecaDoHub("Lanterna", 2.6f, 16.9f, 0f),
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
                    lista.Add(new PecaDoHub("Lanterna", sx * 2.6f, sz * 10.3f, 0f));

                    // Três casas pequenas em volta da praça, de porta para ela (as duas primeiras)
                    // e para a rua em anel (a terceira).
                    AdicionarCasa(lista, casaPequena, sx, sz, 4f, 8f, 0f, -1f);
                    AdicionarCasa(lista, casaPequena, sx, sz, 8f, 4f, -1f, 0f);
                    AdicionarCasa(lista, casaPequena, sx, sz, 8f, 8f, 1f, 0f);

                    // Duas casas largas do outro lado da rua em anel, de porta para ela.
                    AdicionarCasa(lista, casaLarga, sx, sz, 14f, 6f, -1f, 0f);
                    AdicionarCasa(lista, casaLarga, sx, sz, 6f, 14f, 0f, -1f);

                    // Praça do canto: barraca virada para a fonte, árvores e uma carroça ou pedra.
                    var barraca = pedra ? "Barraca_Vermelha" : "Barraca_Verde";
                    var guinada = sx < 0 ? (sz < 0 ? 45f : 135f) : (sz < 0 ? -45f : -135f);
                    lista.Add(new PecaDoHub(barraca, sx * 14.4f, sz * 13.6f, guinada));
                    lista.Add(new PecaDoHub("Arvore_Alta", sx * 16.6f, sz * 16.6f, 40f * (sx + 2)));
                    lista.Add(new PecaDoHub("Arvore", sx * 16.4f, sz * 13.1f, 30f * (sz + 3)));
                    lista.Add(
                        pedra
                            ? new PecaDoHub("Pedra_Pequena", sx * 13.3f, sz * 16.8f, 0f)
                            : new PecaDoHub("Carroca", sx * 13.5f, sz * 16.5f, 0f)
                    );

                    // Árvores no que sobra ao lado das casas largas.
                    lista.Add(new PecaDoHub("Arvore", sx * 16.9f, sz * 4.5f, 70f));
                    lista.Add(new PecaDoHub("Arvore", sx * 16.9f, sz * 7.5f, 110f));
                    lista.Add(new PecaDoHub("Arvore", sx * 4.5f, sz * 16.9f, 160f));
                    lista.Add(new PecaDoHub("Arvore", sx * 7.5f, sz * 16.9f, 200f));
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

        private static IReadOnlyList<PecaDoHub> GerarPropsFora()
        {
            const int quantidade = 46;
            const float distanciaMinimaEntreProps = 4.5f;
            // Folga em volta do quadrado da cidade e do terreno, em metros.
            const float folgaDaCidade = 8f;
            const float folgaDaBorda = 3f;
            const float metadeDoCorredor = 5f;

            var nomes = new[]
            {
                "Arvore",
                "Arvore",
                "Arvore_Alta",
                "Arvore_Alta",
                "Arvore_Torta",
                "Arvore_Redonda",
                "Pedra_Grande",
                "Pedra_Larga",
                "Pedra_Pequena",
            };

            // Gerador congruencial linear próprio: o resultado não depende da plataforma.
            var estado = 20261011u;
            float Proximo()
            {
                estado = unchecked(estado * 1664525u + 1013904223u);
                return (estado >> 8) / 16777216f;
            }

            var lista = new List<PecaDoHub>();
            for (var tentativa = 0; tentativa < 4000 && lista.Count < quantidade; tentativa++)
            {
                var x = (Proximo() * 2f - 1f) * (MetadeDoTerreno - folgaDaBorda);
                var z = (Proximo() * 2f - 1f) * (MetadeDoTerreno - folgaDaBorda);
                var prefab = nomes[(int)(Proximo() * nomes.Length) % nomes.Length];
                var guinada = Proximo() * 360f;

                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < Metade + folgaDaCidade)
                {
                    continue;
                }

                // Corredor da estrada que sai do portão (e o prolongamento dela até a borda).
                if (Mathf.Abs(x) < metadeDoCorredor && z > 0f)
                {
                    continue;
                }

                var perto = false;
                foreach (var outra in lista)
                {
                    var dx = outra.X - x;
                    var dz = outra.Z - z;
                    if (dx * dx + dz * dz < distanciaMinimaEntreProps * distanciaMinimaEntreProps)
                    {
                        perto = true;
                        break;
                    }
                }

                if (!perto)
                {
                    lista.Add(new PecaDoHub(prefab, x, z, guinada));
                }
            }

            return lista;
        }
    }
}
