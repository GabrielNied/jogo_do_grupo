using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Mundo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class CamadasDeTerrenoTestes
    {
        [Test]
        public void Consolidar_SomaPinturaDaCopiaComOriginalEPreservaOutraCamada()
        {
            var dados = new TerrainData { alphamapResolution = 16 };
            var original = new TerrainLayer();
            var copia = new TerrainLayer();
            var outra = new TerrainLayer();
            try
            {
                dados.terrainLayers = new[] { original, outra, copia };
                var pesos = new float[16, 16, 3];
                for (var z = 0; z < 16; z++)
                for (var x = 0; x < 16; x++)
                {
                    pesos[z, x, 0] = x < 8 ? 0.2f : 0.6f;
                    pesos[z, x, 1] = 0.3f;
                    pesos[z, x, 2] = x < 8 ? 0.5f : 0.1f;
                }
                dados.SetAlphamaps(0, 0, pesos);
                var substituicoes = new Dictionary<TerrainLayer, TerrainLayer>
                {
                    [copia] = original,
                };
                OrganizadorDeCamadasDeTerreno.Consolidar(dados, substituicoes);
                // Repetir a correcao nao deve alterar o resultado nem apagar pintura.
                OrganizadorDeCamadasDeTerreno.Consolidar(dados, substituicoes);
                CollectionAssert.AreEqual(new[] { original, outra }, dados.terrainLayers);
                var resultado = dados.GetAlphamaps(0, 0, 16, 16);
                for (var z = 0; z < 16; z++)
                for (var x = 0; x < 16; x++)
                {
                    Assert.That(resultado[z, x, 0], Is.EqualTo(0.7f).Within(0.01f));
                    Assert.That(resultado[z, x, 1], Is.EqualTo(0.3f).Within(0.01f));
                }
            }
            finally
            {
                Object.DestroyImmediate(dados);
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(copia);
                Object.DestroyImmediate(outra);
            }
        }

        [Test]
        public void PavimentoDaPraca_NaoVazaParaForaDosMuros_NoTerrenoPublicado()
        {
            var dados = Resources.Load<TerrainData>("Mundo/TerrenoDoMundo");
            Assert.IsNotNull(dados, "TerrenoDoMundo deve existir em Resources/Mundo.");

            var indicePedra = -1;
            for (var i = 0; i < dados.terrainLayers.Length; i++)
            {
                var nome = dados.terrainLayers[i] != null ? dados.terrainLayers[i].name : "";
                if (nome.Contains("Path_001") || nome.ToLowerInvariant().Contains("stone"))
                {
                    indicePedra = i;
                    break;
                }
            }

            Assert.GreaterOrEqual(indicePedra, 0, "Camada de pedra da praça não encontrada.");

            var alphamap = dados.GetAlphamaps(0, 0, dados.alphamapWidth, dados.alphamapHeight);
            var res = dados.alphamapResolution;
            var tamX = dados.size.x;
            var tamZ = dados.size.z;
            var meioX = tamX * 0.5f;
            var meioZ = tamZ * 0.5f;

            var pontosDentro = 0;
            var vazamentosFora = 0;

            for (var z = 0; z < res; z++)
            {
                var wz = (z / (float)(res - 1)) * tamZ - meioZ;
                for (var x = 0; x < res; x++)
                {
                    var wx = (x / (float)(res - 1)) * tamX - meioX;
                    var pesoPedra = alphamap[z, x, indicePedra];

                    if (Mathf.Abs(wx) <= 30f && Mathf.Abs(wz) <= 30f && pesoPedra > 0.1f)
                    {
                        pontosDentro++;
                    }

                    if ((Mathf.Abs(wx) >= 40f || Mathf.Abs(wz) >= 40f) && pesoPedra > 0.001f)
                    {
                        vazamentosFora++;
                    }
                }
            }

            Assert.Greater(pontosDentro, 0, "O pavimento de pedra deve existir dentro da praça.");
            Assert.AreEqual(
                0,
                vazamentosFora,
                "O pavimento de pedra não pode vazar para fora dos muros (40 m)."
            );
        }

        [Test]
        public void PavimentoDaPraca_NaoVazaParaForaDosMuros_NosBlocos()
        {
            var grade =
                Resources.Load<JogoDoGrupo.Client.Features.Mundo.Terreno.GradeDeTerrenosDoMundo>(
                    JogoDoGrupo
                        .Client
                        .Features
                        .Mundo
                        .Terreno
                        .GradeDeTerrenosDoMundo
                        .CaminhoNosResources
                );
            if (grade == null)
            {
                Assert.Ignore("GradeDeTerrenos não presente nesta configuração.");
                return;
            }

            // Confere os blocos que cobrem a praça e a saída norte
            var b23 = grade.Blocos[
                JogoDoGrupo.Client.Features.Mundo.Terreno.GradeDeTerrenosDoMundo.Indice(-10f, 50f)
            ];
            var b33 = grade.Blocos[
                JogoDoGrupo.Client.Features.Mundo.Terreno.GradeDeTerrenosDoMundo.Indice(10f, 50f)
            ];

            foreach (var bloco in new[] { b23, b33 })
            {
                if (bloco == null)
                    continue;

                var indicePedra = -1;
                for (var i = 0; i < bloco.terrainLayers.Length; i++)
                {
                    var nome = bloco.terrainLayers[i] != null ? bloco.terrainLayers[i].name : "";
                    if (
                        nome.Contains("Path_001")
                        || nome.Contains("Camada_3")
                        || nome.ToLowerInvariant().Contains("stone")
                    )
                    {
                        indicePedra = i;
                        break;
                    }
                }

                if (indicePedra < 0)
                    continue;

                var map = bloco.GetAlphamaps(0, 0, bloco.alphamapWidth, bloco.alphamapHeight);
                var res = bloco.alphamapResolution;

                // Encontra a origem do bloco
                var bx = bloco == b23 ? 2 : 3;
                var bz = 3;
                var origemX = -1500f + bx * 500f;
                var origemZ = -1500f + bz * 500f;

                for (var z = 0; z < res; z++)
                {
                    var wz = origemZ + (z / (float)(res - 1)) * 500f;
                    for (var x = 0; x < res; x++)
                    {
                        var wx = origemX + (x / (float)(res - 1)) * 500f;
                        var peso = map[z, x, indicePedra];

                        // Na saída norte (z de 40 a 70, fora dos muros)
                        if (wz >= 40f && peso > 0.001f)
                        {
                            Assert.Fail(
                                $"Vazamento de pavimento no bloco {bloco.name} em ({wx:F1}, {wz:F1}): peso={peso:F3}"
                            );
                        }
                    }
                }
            }
        }
    }
}
