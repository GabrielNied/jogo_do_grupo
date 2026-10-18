using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Validação de integração no Unity, com assets próprios temporários e Undo real.</summary>
    public static class ValidacaoDoPincelVegetacao
    {
        public static void Executar()
        {
            var pasta =
                "Assets/_Game/Features/Mundo/Editor/ValidacaoPincel" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder(
                "Assets/_Game/Features/Mundo/Editor",
                Path.GetFileName(pasta)
            );
            var dados = new TerrainData
            {
                heightmapResolution = 33,
                size = new Vector3(32f, 10f, 32f),
            };
            dados.SetDetailResolution(32, 8);
            var objeto = Terrain.CreateTerrainGameObject(dados);
            var terreno = objeto.GetComponent<Terrain>();
            ConjuntoDeVegetacao conjunto = null;
            try
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    enableInstancing = true,
                };
                AssetDatabase.CreateAsset(material, pasta + "/Material.asset");
                var arvoreA = CriarModelo(pasta + "/ArvoreA.prefab", material, true);
                var arvoreB = CriarModelo(pasta + "/ArvoreB.prefab", material, true);
                var planta = CriarModelo(pasta + "/Planta.prefab", material, false);
                var grama = new Texture2D(2, 2);
                grama.SetPixels(new[] { Color.green, Color.green, Color.green, Color.green });
                grama.Apply();
                AssetDatabase.CreateAsset(grama, pasta + "/Grama.asset");
                conjunto = ScriptableObject.CreateInstance<ConjuntoDeVegetacao>();
                conjunto.Raio = 10f;
                conjunto.ArvoresPorCemMetros = 10f;
                conjunto.DistanciaEntreArvores = 1f;
                conjunto.Arvores.Add(new EspecieDoPincel { Modelo = arvoreA, Peso = 1f });
                conjunto.Arvores.Add(new EspecieDoPincel { Modelo = arvoreB, Peso = 3f });
                conjunto.Arvores.Add(new EspecieDoPincel { Peso = 0f });
                conjunto.Plantas.Add(new EspecieDoPincel { Modelo = planta });
                conjunto.Gramas.Add(new EspecieDoPincel { Textura = grama });
                Verificar(
                    PinturaDeVegetacao.Validar(conjunto) == null,
                    "Conjunto válido recusado."
                );
                var random = new System.Random(42);
                var escolhasB = 0;
                for (var i = 0; i < 10000; i++)
                {
                    var indice = PinturaDeVegetacao.SortearEspecie(conjunto.Arvores, random);
                    Verificar(indice == 0 || indice == 1, "Espécie com peso zero foi sorteada.");
                    if (indice == 1)
                        escolhasB++;
                }
                Verificar(escolhasB > 7000 && escolhasB < 8000, "Pesos 1:3 não respeitados.");
                AssetDatabase.CreateAsset(conjunto, pasta + "/Conjunto.asset");
                AssetDatabase.SaveAssets();
                var salvo = AssetDatabase.LoadAssetAtPath<ConjuntoDeVegetacao>(
                    pasta + "/Conjunto.asset"
                );
                Verificar(
                    salvo.Arvores[1].Peso == 3f && salvo.Gramas[0].Textura == grama,
                    "Conjunto não conserva pesos e referências."
                );

                var centro = new Vector3(16f, 0f, 16f);
                ValidacaoDoEspacamentoVegetacao.Executar(conjunto, centro);
                Undo.IncrementCurrentGroup();
                var grupo = Undo.GetCurrentGroup();
                PinturaDeVegetacao.Aplicar(
                    terreno,
                    conjunto,
                    centro,
                    false,
                    true,
                    new System.Random(42)
                );
                Undo.CollapseUndoOperations(grupo);
                var quantidade = dados.treeInstanceCount;
                Verificar(quantidade == 31, "Densidade de árvores inesperada.");
                Verificar(
                    dados.detailPrototypes.Length == 2
                        && TotalDetalhes(dados, 0) > 0
                        && TotalDetalhes(dados, 1) > 0,
                    "Plantas e grama não foram pintadas nas camadas nativas."
                );
                var primeira = dados.treeInstances[0].rotation;
                Verificar(
                    Array.Exists(dados.treeInstances, a => Math.Abs(a.rotation - primeira) > 0.1f),
                    "As rotações não variaram."
                );
                Undo.PerformUndo();
                Verificar(
                    dados.treeInstanceCount == 0 && dados.detailPrototypes.Length == 0,
                    "Undo não restaurou o TerrainData inteiro."
                );
                Undo.PerformRedo();
                Verificar(
                    dados.treeInstanceCount == quantidade && TotalDetalhes(dados, 1) > 0,
                    "Redo não restaurou a pintura."
                );
                PinturaDeVegetacao.Aplicar(
                    terreno,
                    conjunto,
                    centro,
                    false,
                    false,
                    new System.Random(7)
                );
                Verificar(
                    dados.treeInstanceCount == quantidade && dados.detailPrototypes.Length == 2,
                    "Repassar o pincel excedeu a densidade ou duplicou protótipos."
                );
                var arvores = dados.treeInstances;
                for (var a = 0; a < arvores.Length; a++)
                for (var b = a + 1; b < arvores.Length; b++)
                    Verificar(
                        Vector3.Distance(
                            Vector3.Scale(arvores[a].position, dados.size),
                            Vector3.Scale(arvores[b].position, dados.size)
                        ) >= 0.999f,
                        "Espaçamento mínimo violado."
                    );
                var estrangeira = CriarModelo(pasta + "/OutraArvore.prefab", material, true);
                var prototipos = new System.Collections.Generic.List<TreePrototype>(
                    dados.treePrototypes
                );
                prototipos.Add(new TreePrototype { prefab = estrangeira });
                dados.treePrototypes = prototipos.ToArray();
                var adicionais = new System.Collections.Generic.List<TreeInstance>(
                    dados.treeInstances
                );
                adicionais.Add(
                    new TreeInstance
                    {
                        prototypeIndex = prototipos.Count - 1,
                        position = new Vector3(0.5f, 0f, 0.5f),
                        widthScale = 1f,
                        heightScale = 1f,
                        color = Color.white,
                        lightmapColor = Color.white,
                    }
                );
                adicionais.Add(
                    new TreeInstance
                    {
                        prototypeIndex = 0,
                        position = Vector3.zero,
                        widthScale = 1f,
                        heightScale = 1f,
                        color = Color.white,
                        lightmapColor = Color.white,
                    }
                );
                dados.treeInstances = adicionais.ToArray();
                dados.SetDetailLayer(
                    0,
                    0,
                    0,
                    new int[,]
                    {
                        { 1 },
                    }
                );
                dados.SetDetailLayer(
                    0,
                    0,
                    1,
                    new int[,]
                    {
                        { 1 },
                    }
                );
                PinturaDeVegetacao.Aplicar(terreno, conjunto, centro, true, false);
                Verificar(
                    dados.treeInstanceCount == 2
                        && Array.Exists(
                            dados.treeInstances,
                            a => a.prototypeIndex == prototipos.Count - 1
                        )
                        && TotalDetalhes(dados, 0) == 1
                        && TotalDetalhes(dados, 1) == 1,
                    "Apagar atingiu outra espécie ou área fora do círculo."
                );
                dados.treeInstances = Array.Empty<TreeInstance>();
                dados.SetDetailLayer(
                    0,
                    0,
                    0,
                    new int[dados.detailResolution, dados.detailResolution]
                );
                dados.SetDetailLayer(
                    0,
                    0,
                    1,
                    new int[dados.detailResolution, dados.detailResolution]
                );
                dados.SetHoles(0, 0, new bool[dados.holesResolution, dados.holesResolution]);
                PinturaDeVegetacao.Aplicar(
                    terreno,
                    conjunto,
                    centro,
                    false,
                    false,
                    new System.Random(42)
                );
                Verificar(
                    dados.treeInstanceCount == 0
                        && TotalDetalhes(dados, 0) == 0
                        && TotalDetalhes(dados, 1) == 0,
                    "Vegetação pintada sobre buracos."
                );
                Debug.Log(
                    "[Mundo] Pincel validado: pesos 1:3, peso zero, conjunto salvo, densidades nativas, rotação, espaçamento, repintura sem duplicação, apagar seletivo com preservação de outras espécies/áreas, buracos, Undo e Redo."
                );
            }
            finally
            {
                Undo.ClearUndo(dados);
                Object.DestroyImmediate(objeto);
                Object.DestroyImmediate(dados);
                if (conjunto != null && !EditorUtility.IsPersistent(conjunto))
                    Object.DestroyImmediate(conjunto);
                AssetDatabase.DeleteAsset(pasta);
            }
        }

        private static int TotalDetalhes(TerrainData dados, int indice)
        {
            var total = 0;
            foreach (
                var valor in dados.GetDetailLayer(
                    0,
                    0,
                    dados.detailResolution,
                    dados.detailResolution,
                    indice
                )
            )
                total += valor;
            return total;
        }

        private static GameObject CriarModelo(string caminho, Material material, bool arvore)
        {
            var objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                objeto.GetComponent<MeshRenderer>().sharedMaterial = material;
                if (arvore)
                {
                    var grupo = objeto.AddComponent<LODGroup>();
                    grupo.SetLODs(
                        new[]
                        {
                            new LOD(0.02f, new Renderer[] { objeto.GetComponent<MeshRenderer>() }),
                        }
                    );
                    grupo.RecalculateBounds();
                }
                return PrefabUtility.SaveAsPrefabAsset(objeto, caminho);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException(mensagem);
        }
    }
}
