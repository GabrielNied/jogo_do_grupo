using JogoDoGrupo.Client.Features.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class PinturaDeEncostasTestes
    {
        [TestCase(2, false)]
        [TestCase(4, false)]
        [TestCase(4, true)]
        public void AplicarComUndo_RestauraPesosECamadas(int quantidade, bool cancelar)
        {
            var dados = new TerrainData
            {
                heightmapResolution = 33,
                alphamapResolution = 64,
                size = new Vector3(32f, 32f, 32f),
            };
            var camadas = new TerrainLayer[quantidade];
            for (var i = 0; i < quantidade; i++)
                camadas[i] = new TerrainLayer();
            var rocha = new TerrainLayer();
            try
            {
                dados.terrainLayers = camadas;
                var alturas = new float[33, 33];
                for (var z = 0; z < 33; z++)
                for (var x = 0; x < 33; x++)
                    alturas[z, x] = x / 32f;
                dados.SetHeights(0, 0, alturas);
                var pesos = new float[64, 64, quantidade];
                for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                    pesos[z, x, 0] = 1f;
                dados.SetAlphamaps(0, 0, pesos);
                if (cancelar)
                    Assert.Throws<System.OperationCanceledException>(() =>
                        PinturaDeEncostas.AplicarComUndo(dados, rocha, 30f, 60f, 1f, p => p > 0f)
                    );
                else
                {
                    PinturaDeEncostas.AplicarComUndo(dados, rocha, 30f, 60f, 1f);
                    Assert.That(
                        dados.GetAlphamaps(32, 32, 1, 1)[0, 0, quantidade],
                        Is.EqualTo(0.5f).Within(0.01f)
                    );
                    Undo.PerformUndo();
                }
                Assert.That(dados.terrainLayers.Length, Is.EqualTo(quantidade));
                Assert.That(dados.GetAlphamaps(32, 8, 1, 1)[0, 0, 0], Is.EqualTo(1f).Within(0.01f));
                if (!cancelar)
                {
                    Undo.PerformRedo();
                    Assert.That(dados.terrainLayers.Length, Is.EqualTo(quantidade + 1));
                    Assert.That(
                        dados.GetAlphamaps(32, 32, 1, 1)[0, 0, quantidade],
                        Is.EqualTo(0.5f).Within(0.01f)
                    );
                }
            }
            finally
            {
                Undo.ClearUndo(dados);
                foreach (var textura in dados.alphamapTextures)
                    Undo.ClearUndo(textura);
                Object.DestroyImmediate(dados);
                foreach (var camada in camadas)
                    Object.DestroyImmediate(camada);
                Object.DestroyImmediate(rocha);
            }
        }

        [TestCase(false, 0f)]
        [TestCase(true, 0.5f)]
        public void Aplicar_PreservaPlanoEMisturaEncostaSemAcumular(
            bool inclinado,
            float rochaEsperada
        )
        {
            var dados = new TerrainData
            {
                heightmapResolution = 33,
                alphamapResolution = 16,
                size = new Vector3(32f, 32f, 32f),
            };
            var grama = new TerrainLayer();
            var terra = new TerrainLayer();
            var rocha = new TerrainLayer();
            try
            {
                dados.terrainLayers = new[] { grama, terra };
                var alturas = new float[33, 33];
                for (var z = 0; z < 33; z++)
                for (var x = 0; x < 33; x++)
                    alturas[z, x] = inclinado ? x / 32f : 0f;
                dados.SetHeights(0, 0, alturas);
                var pesos = new float[16, 16, 2];
                for (var z = 0; z < 16; z++)
                for (var x = 0; x < 16; x++)
                {
                    pesos[z, x, 0] = 0.75f;
                    pesos[z, x, 1] = 0.25f;
                }
                dados.SetAlphamaps(0, 0, pesos);
                PinturaDeEncostas.Aplicar(dados, rocha, 30f, 60f, 1f);
                var primeira = dados.GetAlphamaps(8, 8, 1, 1);
                PinturaDeEncostas.Aplicar(dados, rocha, 30f, 60f, 1f);
                var segunda = dados.GetAlphamaps(8, 8, 1, 1);
                CollectionAssert.AreEqual(new[] { grama, terra, rocha }, dados.terrainLayers);
                Assert.That(segunda[0, 0, 2], Is.EqualTo(rochaEsperada).Within(0.01f));
                Assert.That(
                    segunda[0, 0, 0],
                    Is.EqualTo((1f - rochaEsperada) * 0.75f).Within(0.01f)
                );
                Assert.That(
                    segunda[0, 0, 1],
                    Is.EqualTo((1f - rochaEsperada) * 0.25f).Within(0.01f)
                );
                for (var camada = 0; camada < 3; camada++)
                    Assert.That(
                        segunda[0, 0, camada],
                        Is.EqualTo(primeira[0, 0, camada]).Within(0.005f)
                    );

                // O pincel nativo pode continuar alterando as mesmas alphamaps.
                dados.SetAlphamaps(
                    8,
                    8,
                    new float[,,]
                    {
                        {
                            { 0f, 1f, 0f },
                        },
                    }
                );
                Assert.That(dados.GetAlphamaps(8, 8, 1, 1)[0, 0, 1], Is.EqualTo(1f).Within(0.005f));
            }
            finally
            {
                Object.DestroyImmediate(dados);
                Object.DestroyImmediate(grama);
                Object.DestroyImmediate(terra);
                Object.DestroyImmediate(rocha);
            }
        }
    }
}
