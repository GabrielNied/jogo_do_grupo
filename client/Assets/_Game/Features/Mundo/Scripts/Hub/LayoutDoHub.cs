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
    /// Medidas e disposição do hub. Dados puros (sem criar objetos): o
    /// <see cref="ConstrutorHub"/> instancia o que está aqui. A grade e o tamanho estão
    /// documentados em <c>docs/features/mundo.md</c>.
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

        public const string PrefabChao = "Chao_Hub";
        public const string PrefabMuro = "Muro_Bloco";
        public const string PrefabTorre = "Torre_Canto";
        public const string PrefabEstrada = "Estrada";

        /// <summary>Onde o jogador nasce: pés no chão, ao sul da fonte, olhando para ela (+Z).</summary>
        public static readonly Vector3 PontoDeNascimento = new Vector3(0f, 0f, -12f);

        /// <summary>Para onde o jogador olha ao nascer (+Z).</summary>
        public static readonly Quaternion RotacaoDeNascimento = Quaternion.identity;

        /// <summary>
        /// Props espalhados pelo hub, todos dentro da área andável e fora das estradas em cruz,
        /// exceto a fonte e os bancos da praça.
        /// </summary>
        public static readonly IReadOnlyList<PecaDoHub> Props = new[]
        {
            new PecaDoHub("Fonte", 0f, 0f, 0f),
            // Bancos em volta da fonte.
            new PecaDoHub("Banco", -3.5f, -3.5f, 45f),
            new PecaDoHub("Banco", 3.5f, -3.5f, -45f),
            new PecaDoHub("Banco", -3.5f, 3.5f, -45f),
            new PecaDoHub("Banco", 3.5f, 3.5f, 45f),
            // Lanternas na praça e na entrada das estradas.
            new PecaDoHub("Lanterna", -5.5f, -5.5f, 0f),
            new PecaDoHub("Lanterna", 5.5f, -5.5f, 0f),
            new PecaDoHub("Lanterna", -5.5f, 5.5f, 0f),
            new PecaDoHub("Lanterna", 5.5f, 5.5f, 0f),
            new PecaDoHub("Lanterna", -2.8f, -12f, 0f),
            new PecaDoHub("Lanterna", 2.8f, -12f, 0f),
            new PecaDoHub("Lanterna", -2.8f, 12f, 0f),
            new PecaDoHub("Lanterna", 2.8f, 12f, 0f),
            // Barracas viradas para a fonte.
            new PecaDoHub("Barraca_Vermelha", -8f, -8f, 45f),
            new PecaDoHub("Barraca_Verde", 8f, -8f, -45f),
            new PecaDoHub("Barraca_Verde", -8f, 8f, 135f),
            new PecaDoHub("Barraca_Vermelha", 8f, 8f, -135f),
            // Carroças.
            new PecaDoHub("Carroca", -12f, -7f, 30f),
            new PecaDoHub("Carroca", 12f, 7f, -50f),
            // Árvores nos cantos e junto aos muros.
            new PecaDoHub("Arvore_Alta", -15.5f, -15.5f, 0f),
            new PecaDoHub("Arvore_Alta", 15.5f, -15.5f, 40f),
            new PecaDoHub("Arvore_Alta", -15.5f, 15.5f, 80f),
            new PecaDoHub("Arvore_Alta", 15.5f, 15.5f, 120f),
            new PecaDoHub("Arvore", -15f, -10f, 20f),
            new PecaDoHub("Arvore", 15f, -11f, 60f),
            new PecaDoHub("Arvore", -15f, 11f, 100f),
            new PecaDoHub("Arvore", 15f, 12f, 140f),
            new PecaDoHub("Arvore", -6.5f, 16f, 10f),
            new PecaDoHub("Arvore", 6.5f, 16f, 50f),
            new PecaDoHub("Arvore", -6.5f, -16f, 90f),
            new PecaDoHub("Arvore", 6.5f, -16f, 130f),
            // Pedras.
            new PecaDoHub("Pedra_Grande", -15f, 4.5f, 15f),
            new PecaDoHub("Pedra_Larga", 15f, -4.5f, 75f),
            new PecaDoHub("Pedra_Pequena", 4.5f, 15.5f, 45f),
            new PecaDoHub("Pedra_Pequena", -4.5f, -15.5f, 200f),
        };

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
    }
}
