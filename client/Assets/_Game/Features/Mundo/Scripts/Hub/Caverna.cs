using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// A primeira caverna: um túnel escavado no morro das colinas, contínuo com o terreno (o
    /// furo na boca é do <c>ConstrutorDoTerreno</c>). Cria a malha de rocha com colisão, as
    /// luzes de dentro e as pedras da moldura da boca. Não teleporta ninguém: o jogador entra
    /// andando e o client informa a posição de sempre.
    /// </summary>
    internal static class Caverna
    {
        public const string NomeTunel = "Tunel";
        public const string NomeLuzes = "Luzes";
        public const string NomePedras = "Pedras";

        private static readonly Color CorDaRocha = new Color(0.34f, 0.3f, 0.27f);

        // Pedras da moldura: (deslocamento ao longo da boca, Z à frente da boca, tamanho),
        // dos dois lados da entrada.
        private static readonly float[][] PedrasDaMoldura =
        {
            new[] { -3.6f, 0.5f, 1.6f },
            new[] { 3.5f, 0.8f, 1.8f },
            new[] { -4.8f, 2.6f, 1.3f },
            new[] { 4.7f, 3.1f, 1.4f },
            new[] { -3.9f, -1.2f, 1.1f },
            new[] { 4.1f, -1.0f, 1.2f },
        };

        public static void Montar(Transform pai)
        {
            foreach (var _ in MontarPorEtapas(pai, new OrcamentoDeQuadro(float.MaxValue))) { }
        }

        /// <summary>
        /// Mesma montagem, cedendo a vez (<c>yield</c>) entre o túnel, cada luz e cada pedra
        /// quando o orçamento do quadro esgota.
        /// </summary>
        public static IEnumerable<int> MontarPorEtapas(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var raiz = new GameObject(ConstrutorHub.NomeCaverna);
            raiz.transform.SetParent(pai, false);

            CriarTunel(raiz.transform);
            if (orcamento.Esgotou())
            {
                yield return 0;
            }

            foreach (var passo in CriarLuzes(raiz.transform, orcamento))
            {
                yield return passo;
            }

            foreach (var passo in CriarPedras(raiz.transform, orcamento))
            {
                yield return passo;
            }
        }

        private static void CriarTunel(Transform pai)
        {
            var malha = GeometriaDaCaverna.CriarMalha();
            var tunel = new GameObject(NomeTunel);
            tunel.transform.SetParent(pai, false);
            tunel.AddComponent<MeshFilter>().sharedMesh = malha;
            var renderizador = tunel.AddComponent<MeshRenderer>();
            var material = CriarMaterial(CorDaRocha);
            if (material != null)
            {
                renderizador.sharedMaterial = material;
            }

            tunel.AddComponent<MeshCollider>().sharedMesh = malha;
        }

        // Uma luz de ponto de tempos em tempos ao longo do percurso, uma no meio de cada câmara
        // e uma na ponta do beco. Sem sombra (barato) e sem depender do sol.
        private static IEnumerable<int> CriarLuzes(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var luzes = new GameObject(NomeLuzes).transform;
            luzes.SetParent(pai, false);

            var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
            for (
                var d = ConfigIluminacao.DistanciaEntreLuzesDaCaverna * 0.5f;
                d < comprimento;
                d += ConfigIluminacao.DistanciaEntreLuzesDaCaverna
            )
            {
                PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);
                CriarLuz(luzes, x, z);
                if (orcamento.Esgotou())
                {
                    yield return 0;
                }
            }

            CriarLuz(luzes, PrimeiraCaverna.CamaraFinalX, PrimeiraCaverna.CamaraFinalZ);
            if (orcamento.Esgotou())
            {
                yield return 0;
            }

            CriarLuz(luzes, PrimeiraCaverna.PontaDoBecoX, PrimeiraCaverna.PontaDoBecoZ);
        }

        private static void CriarLuz(Transform pai, float x, float z)
        {
            var piso = PrimeiraCaverna.PisoNoPlano(x, z) ?? PrimeiraCaverna.PisoProximo(x, z);
            var objeto = new GameObject("Luz");
            objeto.transform.SetParent(pai, false);
            objeto.transform.position = new Vector3(
                x,
                piso + ConfigIluminacao.AlturaDaLuzDaCaverna,
                z
            );
            var luz = objeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = ConfigIluminacao.CorDaLuzDaCaverna;
            luz.intensity = ConfigIluminacao.IntensidadeDaLuzDaCaverna;
            luz.range = ConfigIluminacao.AlcanceDaLuzDaCaverna;
            luz.shadows = LightShadows.None;
        }

        // Pedras soltas dos dois lados da boca, apoiadas no chão e sem colisão (não fecham a
        // passagem nem atrapalham o corte do terreno). Posição fixa: o mundo é o mesmo para todos.
        private static IEnumerable<int> CriarPedras(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var pedras = new GameObject(NomePedras).transform;
            pedras.SetParent(pai, false);

            var material = CriarMaterial(CorDaRocha * 0.85f);
            for (var k = 0; k < PedrasDaMoldura.Length; k++)
            {
                var dados = PedrasDaMoldura[k];
                var x = PrimeiraCaverna.EntradaX + dados[0];
                var z = PrimeiraCaverna.EntradaZ + dados[1];
                var tamanho = dados[2];
                var pedra = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedra.name = "Pedra";
                pedra.transform.SetParent(pedras, false);
                pedra.transform.position = new Vector3(
                    x,
                    MapaDoMundo.AlturaEm(x, z) + (tamanho * 0.3f),
                    z
                );
                pedra.transform.rotation = Quaternion.Euler(8f * k, 37f * k, 5f * k);
                pedra.transform.localScale = new Vector3(tamanho, tamanho * 0.7f, tamanho * 0.9f);
                Object.DestroyImmediate(pedra.GetComponent<Collider>());
                if (material != null)
                {
                    pedra.GetComponent<MeshRenderer>().sharedMaterial = material;
                }

                if (orcamento.Esgotou())
                {
                    yield return 0;
                }
            }
        }

        private static Material CriarMaterial(Color cor)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { color = cor };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", cor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.08f);
            }

            return material;
        }
    }
}
