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
    }
}
