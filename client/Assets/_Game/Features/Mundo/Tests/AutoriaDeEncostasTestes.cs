using System;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public sealed class AutoriaDeEncostasTestes
    {
        private string _pasta;
        private Terrain[] _terrenos;
        private TerrainLayer _solo,
            _rocha;
        private bool _pastaBasesExistia;
        private const string PastaBases = "Assets/_Game/Features/Mundo/Editor/BasesDeEncostas";

        [SetUp]
        public void Preparar()
        {
            _pastaBasesExistia = AssetDatabase.IsValidFolder(PastaBases);
            _pasta = "Assets/__EncostasTeste_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", _pasta.Substring("Assets/".Length));
            _solo = new TerrainLayer { diffuseTexture = Texture2D.whiteTexture };
            _rocha = new TerrainLayer { diffuseTexture = Texture2D.whiteTexture };
            AssetDatabase.CreateAsset(_solo, _pasta + "/solo.terrainlayer");
            AssetDatabase.CreateAsset(_rocha, _pasta + "/rocha.terrainlayer");
            _terrenos = Enumerable
                .Range(0, 2)
                .Select(i =>
                {
                    var dados = new TerrainData
                    {
                        heightmapResolution = 33,
                        alphamapResolution = 16,
                        size = new Vector3(32, 32, 32),
                    };
                    dados.terrainLayers = new[] { _solo };
                    var pesos = new float[16, 16, 1];
                    for (var z = 0; z < 16; z++)
                    for (var x = 0; x < 16; x++)
                        pesos[z, x, 0] = 1f;
                    dados.SetAlphamaps(0, 0, pesos);
                    AssetDatabase.CreateAsset(dados, _pasta + "/terreno" + i + ".asset");
                    var objeto = Terrain.CreateTerrainGameObject(dados);
                    objeto.transform.position = new Vector3(i * 32, 50, 0);
                    return objeto.GetComponent<Terrain>();
                })
                .ToArray();
        }

        [TearDown]
        public void Limpar()
        {
            if (_terrenos != null)
                foreach (var terreno in _terrenos)
                {
                    if (terreno == null)
                        continue;
                    var dados = terreno.terrainData;
                    var registro = BaseDeEncostas.Obter(dados, false);
                    Undo.ClearUndo(dados);
                    foreach (var textura in dados.alphamapTextures)
                        Undo.ClearUndo(textura);
                    if (registro != null)
                    {
                        Undo.ClearUndo(registro);
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(registro));
                    }
                    Object.DestroyImmediate(terreno.gameObject);
                }
            if (!string.IsNullOrEmpty(_pasta))
                AssetDatabase.DeleteAsset(_pasta);
            if (
                !_pastaBasesExistia
                && AssetDatabase.IsValidFolder(PastaBases)
                && !System.IO.Directory.EnumerateFileSystemEntries(PastaBases).Any()
            )
                AssetDatabase.DeleteAsset(PastaBases);
        }

        private ConfigDeEncostas Receita() =>
            new ConfigDeEncostas
            {
                Camada = _rocha,
                UsarInclinacao = false,
                UsarAltitude = true,
                AltitudeInicial = 0,
                AltitudePlena = 100,
            };

        [Test]
        public void AltitudeMundial_ReaplicaReduzindoEUndoRedoPreservaHash()
        {
            var receita = Receita();
            PinturaDeEncostas.AplicarLote(_terrenos, receita, false);
            Assert.That(Peso(_terrenos[0]), Is.EqualTo(0.5f).Within(0.01f));
            receita.AltitudePlena = 200;
            PinturaDeEncostas.AplicarLote(_terrenos, receita, false);
            Assert.That(Peso(_terrenos[0]), Is.EqualTo(0.15625f).Within(0.01f));
            Undo.PerformUndo();
            Assert.That(Peso(_terrenos[0]), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(
                BaseDeEncostas
                    .Obter(_terrenos[0].terrainData, false)
                    .Confere(_terrenos[0].terrainData),
                Is.True
            );
            Undo.PerformRedo();
            Assert.That(Peso(_terrenos[0]), Is.EqualTo(0.15625f).Within(0.01f));
            Assert.That(
                BaseDeEncostas
                    .Obter(_terrenos[0].terrainData, false)
                    .Confere(_terrenos[0].terrainData),
                Is.True
            );
            PinturaDeEncostas.AplicarLote(_terrenos, receita, true);
            CollectionAssert.AreEqual(new[] { _solo }, _terrenos[0].terrainData.terrainLayers);
        }

        [Test]
        public void RetoqueBloqueiaTodoLoteAntesDeAlterarPrimeiroTerreno()
        {
            var receita = Receita();
            PinturaDeEncostas.AplicarLote(_terrenos, receita, false);
            _terrenos[1]
                .terrainData.SetAlphamaps(
                    0,
                    0,
                    new float[,,]
                    {
                        {
                            { 1f, 0f },
                        },
                    }
                );
            var antes = BaseDeEncostas.Assinatura(_terrenos[0].terrainData);
            receita.CoberturaAltitude = 0;
            Assert.Throws<InvalidOperationException>(() =>
                PinturaDeEncostas.AplicarLote(_terrenos, receita, false)
            );
            Assert.That(BaseDeEncostas.Assinatura(_terrenos[0].terrainData), Is.EqualTo(antes));
            Assert.That(_terrenos[1].terrainData.GetAlphamaps(0, 0, 1, 1)[0, 0, 0], Is.EqualTo(1));
        }

        [Test]
        public void CancelarSegundoTerreno_RevertePrimeiroECamadas()
        {
            var antes = _terrenos.Select(t => BaseDeEncostas.Assinatura(t.terrainData)).ToArray();
            Assert.Throws<OperationCanceledException>(() =>
                PinturaDeEncostas.AplicarLote(_terrenos, Receita(), false, p => p >= 0.5f)
            );
            CollectionAssert.AreEqual(
                antes,
                _terrenos.Select(t => BaseDeEncostas.Assinatura(t.terrainData)).ToArray()
            );
            Assert.That(BaseDeEncostas.Obter(_terrenos[0].terrainData, false), Is.Null);
            _terrenos[0].terrainData.terrainLayers = new[] { _solo, _rocha };
            Assert.DoesNotThrow(() => PinturaDeEncostas.AplicarLote(_terrenos, Receita(), false));
        }

        [Test]
        public void MoverRegiaoPreservaPinturaAnteriorForaDoNovoCirculo()
        {
            var receita = Receita();
            receita.LimitarRegiao = true;
            receita.Centro = new Vector2(5, 5);
            receita.Raio = 4;
            receita.Borda = 0;
            PinturaDeEncostas.AplicarLote(_terrenos, receita, false);
            var dado = _terrenos[0].terrainData;
            var anterior = dado.GetAlphamaps(2, 2, 1, 1)[0, 0, 1];
            Assert.That(anterior, Is.EqualTo(0.5f).Within(0.01f));
            receita.Centro = new Vector2(25, 25);
            PinturaDeEncostas.AplicarLote(_terrenos, receita, false);
            Assert.That(dado.GetAlphamaps(2, 2, 1, 1)[0, 0, 1], Is.EqualTo(anterior));
            Assert.That(dado.GetAlphamaps(12, 12, 1, 1)[0, 0, 1], Is.EqualTo(0.5f).Within(0.01f));
            PinturaDeEncostas.AplicarLote(_terrenos, receita, true);
            Assert.That(dado.GetAlphamaps(2, 2, 1, 1)[0, 0, 1], Is.EqualTo(anterior));
            Assert.That(dado.GetAlphamaps(12, 12, 1, 1)[0, 0, 1], Is.Zero.Within(0.01f));
        }

        [Test]
        public void ParedeForteSubdivideEspacamentoNaSuperficie()
        {
            var pintura = Receita();
            pintura.CoberturaAltitude = 1;
            pintura.AltitudeInicial = -100;
            pintura.AltitudePlena = 0;
            var rochas = new ConfigDeRochas
            {
                Modelos = new[] { _terrenos[0].gameObject },
                InclinacaoMinima = 0,
                Espacamento = 5,
                Limite = 2000,
                Densidade = 1,
            };
            var alvo = new[] { _terrenos[0] };
            var plano = RevestimentoDeEncostas.Planejar(alvo, pintura, rochas);
            var dados = alvo[0].terrainData;
            dados.size = new Vector3(32, 200, 32);
            var alturas = new float[33, 33];
            for (var z = 0; z < 33; z++)
            for (var x = 0; x < 33; x++)
                alturas[z, x] = x / 32f * 0.9f;
            dados.SetHeights(0, 0, alturas);
            var parede = RevestimentoDeEncostas.Planejar(alvo, pintura, rochas);
            Assert.That(
                parede.Count,
                Is.GreaterThan(plano.Count * 3),
                "Parede80 deve receber faixas ao longo da altura."
            );
            for (var i = 0; i < parede.Count; i++)
            for (var j = i + 1; j < parede.Count; j++)
                Assert.That(
                    Vector3.Distance(parede[i].Ponto, parede[j].Ponto),
                    Is.GreaterThanOrEqualTo(rochas.Espacamento * 0.7f - 0.001f)
                );
        }

        [Test]
        public void RochasGeradasPreservamRegiaoExternaUndoEPivoSemColisao()
        {
            var fonte = new GameObject("Modelo com pivo deslocado");
            GameObject modelo;
            try
            {
                fonte.transform.localScale = new Vector3(0.5f, 1f, 2f);
                var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubo.transform.SetParent(fonte.transform, false);
                cubo.transform.localPosition = new Vector3(2f, 1f, -3f);
                cubo.transform.localScale = new Vector3(2f, 0.3f, 0.7f);
                fonte.AddComponent<Rigidbody>();
                modelo = PrefabUtility.SaveAsPrefabAsset(fonte, _pasta + "/modelo.prefab");
            }
            finally
            {
                Object.DestroyImmediate(fonte);
            }
            var pintura = Receita();
            pintura.AltitudeInicial = -100;
            pintura.AltitudePlena = 0;
            var rochas = new ConfigDeRochas
            {
                Modelos = new[] { modelo },
                InclinacaoMinima = 0,
                Espacamento = 5,
                Limite = 30,
                Densidade = 1,
                Tamanho = new Vector2(4, 4),
                AcompanharNormal = 0,
            };
            var total = RevestimentoDeEncostas.Aplicar(_terrenos, pintura, rochas);
            Assert.That(total, Is.GreaterThan(2));
            var grupo = _terrenos[0].GetComponentInChildren<RochasGeradasDeEncosta>();
            Assert.That(grupo.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(grupo.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            Assert.That(modelo.GetComponentsInChildren<Collider>(), Is.Not.Empty);
            var primeira = grupo.Pecas[0];
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(primeira.Objeto), Is.False);
            var bounds = primeira.Objeto.GetComponentInChildren<Renderer>().bounds;
            Assert.That(
                Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
                Is.EqualTo(4).Within(0.01f)
            );
            pintura.LimitarRegiao = true;
            pintura.Centro = primeira.Ancora;
            pintura.Raio = 2;
            pintura.Borda = 0;
            var fora = grupo
                .Pecas.Where(p => pintura.PesoRegiao(p.Ancora.x, p.Ancora.y) == 0)
                .Select(p => p.Objeto)
                .ToArray();
            Assert.That(fora.Length, Is.GreaterThan(0));
            RevestimentoDeEncostas.Aplicar(_terrenos, pintura, rochas);
            Assert.That(fora.All(o => o != null), Is.True);
            Vector3[] Estado() =>
                _terrenos
                    .SelectMany(t => t.GetComponentsInChildren<Renderer>())
                    .SelectMany(r =>
                        new[] { r.bounds.center, r.bounds.size, r.transform.eulerAngles }
                    )
                    .ToArray();
            var estado = Estado();
            Undo.PerformUndo();
            Assert.That(
                _terrenos.Sum(t => t.GetComponentsInChildren<Renderer>().Length),
                Is.GreaterThanOrEqualTo(total)
            );
            Undo.PerformRedo();
            CollectionAssert.AreEqual(estado, Estado());
            Assert.That(
                _terrenos.All(t =>
                    t.GetComponentsInChildren<Collider>().All(c => c is TerrainCollider)
                ),
                Is.True
            );
            Assert.That(
                _terrenos.All(t => t.GetComponentsInChildren<Rigidbody>().Length == 0),
                Is.True
            );
            Assert.That(
                _terrenos.All(t =>
                    t.GetComponentsInChildren<MonoBehaviour>().All(m => m is RochasGeradasDeEncosta)
                ),
                Is.True
            );
        }

        [Test]
        public void MascaraCombinaMaximoComMundoContinuoEBorda()
        {
            var receita = Receita();
            receita.UsarInclinacao = true;
            receita.Inicio = 30;
            receita.Plena = 60;
            receita.VariacaoAltitude = 8;
            receita.VariacaoAngulo = 5;
            var mundoA = _terrenos[0].transform.position + new Vector3(32, 0, 8);
            var mundoB = _terrenos[1].transform.position + new Vector3(0, 0, 8);
            Assert.That(receita.Mascara(mundoA, 45), Is.EqualTo(receita.Mascara(mundoB, 45)));
            receita.VariacaoAltitude = receita.VariacaoAngulo = 0;
            Assert.That(receita.Mascara(new Vector3(0, 100, 0), 0), Is.EqualTo(1));
            Assert.That(receita.Mascara(new Vector3(0, 0, 0), 90), Is.EqualTo(1));
            receita.LimitarRegiao = true;
            receita.Raio = 10;
            receita.Borda = 2;
            Assert.That(receita.Mascara(new Vector3(11, 100, 0), 90), Is.Zero);
        }

        [Test]
        public void BuracosEPesosSaoPreservadosEPreviaNaoSujaDados()
        {
            var dados = _terrenos[0].terrainData;
            var buracos = new bool[dados.holesResolution, dados.holesResolution];
            for (var z = 0; z < buracos.GetLength(0); z++)
            for (var x = 0; x < buracos.GetLength(1); x++)
                buracos[z, x] = x >= dados.holesResolution / 2;
            dados.SetHoles(0, 0, buracos);
            var antes = BaseDeEncostas.Assinatura(dados);
            using (var previa = new PreviaDeEncostas())
                previa.Atualizar(_terrenos, Receita(), null);
            Assert.That(BaseDeEncostas.Assinatura(dados), Is.EqualTo(antes));
            PinturaDeEncostas.AplicarLote(_terrenos, Receita(), false);
            var pesos = dados.GetAlphamaps(0, 0, 16, 16);
            Assert.That(pesos[8, 0, 1], Is.Zero.Within(0.005f));
            Assert.That(pesos[8, 15, 1], Is.EqualTo(0.5f).Within(0.01f));
            for (var z = 0; z < 16; z++)
            for (var x = 0; x < 16; x++)
                Assert.That(pesos[z, x, 0] + pesos[z, x, 1], Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void RochasRespeitamZeroLimiteEDeterminismo()
        {
            var pintura = Receita();
            var rochas = new ConfigDeRochas
            {
                Modelos = new[] { _terrenos[0].gameObject },
                InclinacaoMinima = 0,
                Espacamento = 2,
                Limite = 3,
                Densidade = 0,
            };
            Assert.That(RevestimentoDeEncostas.Planejar(_terrenos, pintura, rochas), Is.Empty);
            rochas.Densidade = 1;
            var primeira = RevestimentoDeEncostas.Planejar(_terrenos, pintura, rochas);
            var segunda = RevestimentoDeEncostas.Planejar(
                _terrenos.Reverse().ToArray(),
                pintura,
                rochas
            );
            Assert.That(primeira.Count, Is.EqualTo(3));
            CollectionAssert.AreEqual(
                primeira.Select(p => p.Ponto).ToArray(),
                segunda.Select(p => p.Ponto).ToArray()
            );
            CollectionAssert.AreEqual(
                primeira.Select(p => p.Tamanho).ToArray(),
                segunda.Select(p => p.Tamanho).ToArray()
            );
        }

        private static float Peso(Terrain terreno) =>
            terreno.terrainData.GetAlphamaps(8, 8, 1, 1)[0, 0, 1];
    }
}
