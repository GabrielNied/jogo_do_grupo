using System;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Editor;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class GradeDeTerrenosTestes
    {
        [TestCase(-1500f, -1500f, 0)]
        [TestCase(-1000f, -1500f, 1)]
        [TestCase(0f, 0f, 21)]
        [TestCase(1500f, 1500f, 35)]
        [TestCase(1500f, -1500f, 5)]
        public void Consulta_escolhe_o_bloco_incluindo_divisas_e_limite_externo(
            float x,
            float z,
            int indice
        )
        {
            Assert.AreEqual(indice, GradeDeTerrenosDoMundo.Indice(x, z));
        }

        [Test]
        public void Consulta_recusa_fora_do_mundo()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GradeDeTerrenosDoMundo.Indice(1501f, 0f)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GradeDeTerrenosDoMundo.Indice(0f, float.NaN)
            );
        }

        [Test]
        public void Reamostragem_preserva_as_diagonais_das_encostas()
        {
            var origem = new TerrainData
            {
                heightmapResolution = 33,
                size = new Vector3(3000f, 100f, 3000f),
            };
            var destino = new TerrainData
            {
                heightmapResolution = 1025,
                size = new Vector3(500f, 100f, 500f),
            };
            try
            {
                var alturas = new float[33, 33];
                for (var z = 0; z < 33; z++)
                for (var x = 0; x < 33; x++)
                    alturas[z, x] = (x + z) % 2 == 0 ? 0.1f : 0.8f;
                origem.SetHeights(0, 0, alturas);
                destino.SetHeights(0, 0, DivisorDoTerrenoEmBlocos.ReamostrarAlturas(origem, 3, 3));
                for (var z = 0; z <= 20; z++)
                for (var x = 0; x <= 20; x++)
                {
                    var nx = x / 20f;
                    var nz = z / 20f;
                    Assert.AreEqual(
                        origem.GetInterpolatedHeight((3f + nx) / 6f, (3f + nz) / 6f),
                        destino.GetInterpolatedHeight(nx, nz),
                        0.01f
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(origem);
                Object.DestroyImmediate(destino);
            }
        }

        [Test]
        public void Ferramentas_nativas_atravessam_o_encontro_de_quatro_blocos()
        {
            ValidacaoDosBlocosDeTerreno.Executar();
        }

        [Test]
        public void Mapa_publicado_carrega_copias_e_vizinhos_dos_36_blocos()
        {
            var grade = Resources.Load<GradeDeTerrenosDoMundo>(
                GradeDeTerrenosDoMundo.CaminhoNosResources
            );
            if (grade == null)
                Assert.Ignore("A migração real ainda não foi executada nesta cópia do projeto.");
            var mapa = ConstrutorHub.InstanciarMapaPublicadoParaTeste();
            Assert.IsNotNull(mapa);
            var terrenos = mapa.GetComponentsInChildren<Terrain>();
            var copias = terrenos.Select(t => t.terrainData).ToArray();
            try
            {
                Assert.AreEqual(36, terrenos.Length);
                var conexoes = 0;
                foreach (var terreno in terrenos)
                {
                    var posicao = terreno.transform.position;
                    var original = grade.Blocos[
                        GradeDeTerrenosDoMundo.Indice(posicao.x, posicao.z)
                    ];
                    Assert.AreNotSame(original, terreno.terrainData);
                    Assert.AreSame(
                        terreno.terrainData,
                        terreno.GetComponent<TerrainCollider>().terrainData
                    );
                    Assert.AreEqual(500f, terreno.terrainData.size.x);
                    if (terreno.leftNeighbor != null)
                        conexoes++;
                    if (terreno.topNeighbor != null)
                        conexoes++;
                    if (terreno.rightNeighbor != null)
                        conexoes++;
                    if (terreno.bottomNeighbor != null)
                        conexoes++;
                }
                Assert.AreEqual(
                    120,
                    conexoes,
                    "As 60 divisas precisam estar conectadas nos dois sentidos."
                );
            }
            finally
            {
                Object.DestroyImmediate(mapa);
                foreach (var dados in copias)
                    if (dados != null && !UnityEditor.EditorUtility.IsPersistent(dados))
                        Object.DestroyImmediate(dados);
            }
        }
    }
}
