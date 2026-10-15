using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Quanto de cada material de chão existe num ponto: grama, terra, areia, neve e lama (somam
    /// 1). A rocha não entra aqui: o shader a mistura pela inclinação. Contas puras: o
    /// <see cref="ConstrutorDoTerreno"/> grava o resultado numa textura de pesos, e é ela (e não
    /// a cor macro) que decide o material de cada ponto.
    /// </summary>
    public readonly struct PesosDoTerreno
    {
        /// <summary>Parte da terra batida nas colinas (o resto é grama seca).</summary>
        public const float TerraNasColinas = 0.6f;

        /// <summary>Altura acima da linha d'água em que a margem vira areia (m).</summary>
        public const float AlturaDaPraia = 0.85f;

        /// <summary>Profundidade abaixo da linha d'água em que o fundo vira lama (m).</summary>
        public const float ProfundidadeDoLodo = 0.3f;

        /// <summary>Distância do rio até onde a margem é de areia e terra (m).</summary>
        public const float LarguraDaMargemDeRio = 14f;

        /// <summary>Parte da margem de rio que é areia (o resto é terra úmida).</summary>
        public const float AreiaNaMargemDeRio = 0.6f;

        public PesosDoTerreno(float grama, float terra, float areia, float neve, float lama)
        {
            Grama = grama;
            Terra = terra;
            Areia = areia;
            Neve = neve;
            Lama = lama;
        }

        public float Grama { get; }

        public float Terra { get; }

        public float Areia { get; }

        public float Neve { get; }

        public float Lama { get; }

        public float Soma => Grama + Terra + Areia + Neve + Lama;

        /// <summary>
        /// Pesos do chão em (<paramref name="x"/>, <paramref name="z"/>) a <paramref name="altura"/>
        /// metros: o bioma decide a base, a margem da água vira areia, o fundo do pântano vira
        /// lama, a beira dos rios vira areia e terra, e a lava e a estrada ficam de terra.
        /// </summary>
        public static PesosDoTerreno Calcular(PesosDosBiomas biomas, float altura, float x, float z)
        {
            var p = new float[5]; // grama, terra, areia, neve, lama
            p[0] = biomas.Campo + biomas.Mata + biomas.Colinas * (1f - TerraNasColinas);
            p[1] = biomas.Colinas * TerraNasColinas + biomas.Lava;
            p[2] = biomas.Deserto;
            p[3] = biomas.Neve;
            p[4] = biomas.Pantano;

            // Praia: a margem do pântano e das lagoas, logo acima da linha d'água.
            var acima = altura - MapaDoMundo.NivelDaAgua;
            if (biomas.Pantano > 0.05f && acima >= 0f && acima <= AlturaDaPraia)
            {
                var praia = 1f - Mathf.Clamp01(Mathf.Abs(acima - 0.2f) / 0.65f);
                praia = praia * praia * (3f - 2f * praia) * 0.95f;
                Misturar(p, 2, praia);
            }

            // Fundo das poças e lagos: lama no lugar de qualquer outro material.
            if (altura < MapaDoMundo.NivelDaAgua && biomas.Lava < 0.5f)
            {
                var fundo = Mathf.Clamp01((MapaDoMundo.NivelDaAgua - altura) / ProfundidadeDoLodo);
                Misturar(p, 4, fundo);
            }

            // Margens de rio: areia e terra úmida onde não há neve nem lava.
            if (
                biomas.Lava < 0.35f
                && biomas.Neve < 0.5f
                && MapaDoMundo.ProximoDeRio(x, z, out var distancia)
            )
            {
                var margem = (1f - Mathf.Clamp01(distancia / LarguraDaMargemDeRio)) * 0.75f;
                Misturar(p, 2, margem * AreiaNaMargemDeRio);
                Misturar(p, 1, margem * (1f - AreiaNaMargemDeRio));
            }

            // Estrada que sai do portão: terra batida.
            if (
                Mathf.Abs(x) <= 8f
                && z >= MapaDoMundo.MetadeDaCidade
                && z <= MapaDoMundo.RaioTransicao2
            )
            {
                var lateral = 1f - Mathf.InverseLerp(4f, 8f, Mathf.Abs(x));
                Misturar(p, 1, lateral * 0.7f);
            }

            var total = p[0] + p[1] + p[2] + p[3] + p[4];
            if (total <= 0.0001f)
            {
                return new PesosDoTerreno(1f, 0f, 0f, 0f, 0f);
            }

            return new PesosDoTerreno(
                p[0] / total,
                p[1] / total,
                p[2] / total,
                p[3] / total,
                p[4] / total
            );
        }

        // Leva a fração "quanto" do total para o material "indice", tirando proporcionalmente
        // dos outros (a soma não muda).
        private static void Misturar(float[] p, int indice, float quanto)
        {
            var total = p[0] + p[1] + p[2] + p[3] + p[4];
            quanto = Mathf.Clamp01(quanto);
            for (var k = 0; k < p.Length; k++)
            {
                p[k] *= 1f - quanto;
            }

            p[indice] += total * quanto;
        }

        /// <summary>
        /// Grava os quatro primeiros pesos nos canais da textura (R grama, G terra, B areia, A
        /// neve). A lama é o que falta para 1.
        /// </summary>
        public Color32 ParaTextura()
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(Grama) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(Terra) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(Areia) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(Neve) * 255f)
            );
        }
    }
}
