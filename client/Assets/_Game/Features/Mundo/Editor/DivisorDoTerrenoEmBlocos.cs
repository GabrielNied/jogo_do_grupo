using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Mundo.Autoria;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Migração única da autoria salva. Não regenera relevo nem dados do servidor.</summary>
    public static class DivisorDoTerrenoEmBlocos
    {
        private const string Pasta = "Assets/_Game/Features/Mundo/Resources/Mundo";
        private const string PastaDosBlocos = Pasta + "/Blocos";
        private const string CaminhoDaGrade = Pasta + "/GradeDeTerrenos.asset";
        private const string Cena = "Assets/_Game/Features/Mundo/Scenes/AutoriaDoMapa.unity";
        private const float ToleranciaDaBorda = 0.001f;

        [MenuItem("Jogo do Grupo/Mundo/Dividir autoria em blocos de 500 m")]
        public static void Dividir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do Play antes de dividir o mapa.");
            if (AssetDatabase.LoadAssetAtPath<GradeDeTerrenosDoMundo>(CaminhoDaGrade) != null)
            {
                Debug.Log(
                    "[Mundo] Autoria já dividida. Abra AutoriaDoMapa; a migração não é reaplicada."
                );
                return;
            }
            // Recusar uma migração parcial em vez de sobrescrever blocos que alguém já editou.
            if (AssetDatabase.IsValidFolder(PastaDosBlocos))
                throw new InvalidOperationException(
                    "A pasta Blocos já existe sem a grade. Confira a migração anterior antes de continuar."
                );
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            var cena = SceneManager.GetActiveScene();
            if (cena.path != Cena)
                cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var mapa = GameObject.Find(ConstrutorHub.NomeRaiz);
            if (mapa == null)
                throw new InvalidOperationException("Raiz Hub ausente na cena de autoria.");
            var terrenos = ExportadorDoMapaFeitoAMao.OrdenarEValidarTerrenos(
                mapa.GetComponentsInChildren<Terrain>()
            );
            if (terrenos.Length != 1)
                throw new InvalidOperationException(
                    "A migração exige exatamente o Terrain original de 3 km."
                );
            var original = terrenos[0];
            var origem = original.terrainData;
            if (
                !AssetDatabase.Contains(origem)
                || original.transform.lossyScale != Vector3.one
                || original.transform.rotation != Quaternion.identity
                || Vector3.Distance(
                    original.transform.position,
                    GradeDeTerrenosDoMundo.Origem(0, 0)
                ) > ToleranciaDaBorda
                || Mathf.Abs(origem.size.y - (MapaDoMundo.AlturaMaxima - MapaDoMundo.AlturaMinima))
                    > ToleranciaDaBorda
            )
                throw new InvalidOperationException(
                    "O terreno original precisa ser persistente, alinhado e sem escala/rotação."
                );

            Debug.Log(
                $"[Mundo] Origem: {origem.heightmapResolution} relevo, {origem.alphamapResolution} pintura, {origem.detailResolution} detalhes, {origem.alphamapLayers} camadas. Árvores e detalhes não serão migrados."
            );
            var alturas = origem.GetHeights(
                0,
                0,
                origem.heightmapResolution,
                origem.heightmapResolution
            );
            var pintura = origem.GetAlphamaps(0, 0, origem.alphamapWidth, origem.alphamapHeight);
            var buracos = origem.GetHoles(0, 0, origem.holesResolution, origem.holesResolution);
            AssetDatabase.CreateFolder(Pasta, "Blocos");
            var camadas = CopiarCamadasCompartilhadas(origem.terrainLayers);
            var grade = ScriptableObject.CreateInstance<GradeDeTerrenosDoMundo>();
            var novos = new Terrain[GradeDeTerrenosDoMundo.Quantidade];
            try
            {
                for (var z = 0; z < GradeDeTerrenosDoMundo.BlocosPorLado; z++)
                for (var x = 0; x < GradeDeTerrenosDoMundo.BlocosPorLado; x++)
                {
                    var indice = z * GradeDeTerrenosDoMundo.BlocosPorLado + x;
                    EditorUtility.DisplayProgressBar(
                        "Dividindo terreno",
                        $"Bloco {x}, {z}",
                        indice / (float)novos.Length
                    );
                    var dados = ReamostrarBloco(origem, alturas, pintura, buracos, camadas, x, z);
                    AssetDatabase.CreateAsset(dados, $"{PastaDosBlocos}/Terreno_{x}_{z}.asset");
                    grade.Blocos[indice] = dados;
                    var objeto = Terrain.CreateTerrainGameObject(dados);
                    objeto.name =
                        indice == 0
                            ? TerrenoNativoDoMundo.Nome
                            : $"{TerrenoNativoDoMundo.Nome}_{x}_{z}";
                    objeto.layer = original.gameObject.layer;
                    objeto.transform.SetParent(original.transform.parent, false);
                    objeto.transform.position = GradeDeTerrenosDoMundo.Origem(x, z);
                    objeto.isStatic = original.gameObject.isStatic;
                    var terreno = objeto.GetComponent<Terrain>();
                    novos[indice] = terreno;
                    CopiarConfiguracao(original, terreno);
                    var configuracao = original.GetComponent<ConfiguracaoDeAutoriaDoMapa>();
                    if (configuracao != null)
                        EditorUtility.CopySerialized(
                            configuracao,
                            objeto.AddComponent<ConfiguracaoDeAutoriaDoMapa>()
                        );
                    var colisor = original.GetComponent<TerrainCollider>();
                    if (colisor != null)
                        objeto.GetComponent<TerrainCollider>().sharedMaterial =
                            colisor.sharedMaterial;
                    EditorUtility.SetDirty(dados);
                }
                GradeDeTerrenosDoMundo.Conectar(novos);
                ValidarBordas(novos);
                // Só troca a cena depois de gerar e conferir todos os blocos.
                Object.DestroyImmediate(original.gameObject);
                AssetDatabase.CreateAsset(grade, CaminhoDaGrade);
                ExportadorDoMapaFeitoAMao.OrdenarEValidarTerrenos(novos);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                ExportadorDoMapaFeitoAMao.SalvarPrefabPublicado();
                Selection.activeGameObject = novos[
                    GradeDeTerrenosDoMundo.Indice(0f, 0f)
                ].gameObject;
                Debug.Log(
                    "[Mundo] 36 blocos de 500 m, relevo 1025, pintura/detalhes 1024. Bordas conferidas. Cena e prefab salvos; relevo e pintura preservados, sem árvores/grama. Dados do servidor não republicados."
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // As mesmas TerrainLayers em todos os blocos permitem pintar através das divisas.
        // Um número inteiro de repetições por 500 m evita reiniciar a textura com outra fase.
        private static TerrainLayer[] CopiarCamadasCompartilhadas(TerrainLayer[] originais)
        {
            var camadas = new TerrainLayer[originais.Length];
            var copias = new Dictionary<TerrainLayer, TerrainLayer>();
            for (var i = 0; i < originais.Length; i++)
            {
                if (originais[i] == null)
                    throw new InvalidOperationException("Camada de pintura ausente: " + i);
                if (!copias.TryGetValue(originais[i], out var camada))
                {
                    camada = Object.Instantiate(originais[i]);
                    camada.name = originais[i].name;
                    camada.tileSize = new Vector2(
                        AjustarRepeticao(camada.tileSize.x),
                        AjustarRepeticao(camada.tileSize.y)
                    );
                    AssetDatabase.CreateAsset(camada, $"{PastaDosBlocos}/Camada_{i}.terrainlayer");
                    copias.Add(originais[i], camada);
                }
                camadas[i] = camada;
            }
            return camadas;
        }

        private static float AjustarRepeticao(float tamanho) =>
            GradeDeTerrenosDoMundo.LadoDoBloco
            / (float)
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        GradeDeTerrenosDoMundo.LadoDoBloco / Mathf.Max(0.001f, tamanho)
                    )
                );

        public static TerrainData ReamostrarBloco(
            TerrainData origem,
            float[,] alturas,
            float[,,] pintura,
            bool[,] buracos,
            TerrainLayer[] camadas,
            int blocoX,
            int blocoZ
        )
        {
            var dados = new TerrainData
            {
                name = $"Terreno_{blocoX}_{blocoZ}",
                heightmapResolution = GradeDeTerrenosDoMundo.ResolucaoDoRelevo,
                alphamapResolution = GradeDeTerrenosDoMundo.ResolucaoDaPintura,
                baseMapResolution = GradeDeTerrenosDoMundo.ResolucaoDaPintura,
                size = new Vector3(
                    GradeDeTerrenosDoMundo.LadoDoBloco,
                    origem.size.y,
                    GradeDeTerrenosDoMundo.LadoDoBloco
                ),
                terrainLayers = camadas,
                treePrototypes = origem.treePrototypes,
                detailPrototypes = origem.detailPrototypes,
                wavingGrassAmount = origem.wavingGrassAmount,
                wavingGrassSpeed = origem.wavingGrassSpeed,
                wavingGrassStrength = origem.wavingGrassStrength,
                wavingGrassTint = origem.wavingGrassTint,
                enableHolesTextureCompression = origem.enableHolesTextureCompression,
            };
            dados.SetDetailScatterMode(origem.detailScatterMode);
            dados.SetDetailResolution(
                GradeDeTerrenosDoMundo.ResolucaoDosDetalhes,
                GradeDeTerrenosDoMundo.DetalhesPorPatch
            );
            var resolucao = dados.heightmapResolution;
            var novasAlturas = new float[resolucao, resolucao];
            for (var z = 0; z < resolucao; z++)
            for (var x = 0; x < resolucao; x++)
                novasAlturas[z, x] = Amostrar(
                    alturas,
                    Coordenada(blocoX, x, resolucao),
                    Coordenada(blocoZ, z, resolucao)
                );
            dados.SetHeights(0, 0, novasAlturas);
            resolucao = dados.alphamapResolution;
            var novaPintura = new float[resolucao, resolucao, camadas.Length];
            for (var z = 0; z < resolucao; z++)
            for (var x = 0; x < resolucao; x++)
            {
                var px = Coordenada(blocoX, x, resolucao) * (pintura.GetLength(1) - 1);
                var pz = Coordenada(blocoZ, z, resolucao) * (pintura.GetLength(0) - 1);
                var x0 = Mathf.FloorToInt(px);
                var z0 = Mathf.FloorToInt(pz);
                var x1 = Mathf.Min(x0 + 1, pintura.GetLength(1) - 1);
                var z1 = Mathf.Min(z0 + 1, pintura.GetLength(0) - 1);
                var soma = 0f;
                for (var i = 0; i < camadas.Length; i++)
                {
                    var valor = Mathf.Lerp(
                        Mathf.Lerp(pintura[z0, x0, i], pintura[z0, x1, i], px - x0),
                        Mathf.Lerp(pintura[z1, x0, i], pintura[z1, x1, i], px - x0),
                        pz - z0
                    );
                    novaPintura[z, x, i] = valor;
                    soma += valor;
                }
                if (soma > 0f)
                    for (var i = 0; i < camadas.Length; i++)
                        novaPintura[z, x, i] /= soma;
                else if (camadas.Length > 0)
                    novaPintura[z, x, 0] = 1f;
            }
            if (camadas.Length > 0)
                dados.SetAlphamaps(0, 0, novaPintura);
            resolucao = dados.holesResolution;
            var novosBuracos = new bool[resolucao, resolucao];
            for (var z = 0; z < resolucao; z++)
            for (var x = 0; x < resolucao; x++)
            {
                var bx = Mathf.Min(
                    buracos.GetLength(1) - 1,
                    Mathf.FloorToInt(
                        (blocoX + (x + 0.5f) / resolucao)
                            / GradeDeTerrenosDoMundo.BlocosPorLado
                            * buracos.GetLength(1)
                    )
                );
                var bz = Mathf.Min(
                    buracos.GetLength(0) - 1,
                    Mathf.FloorToInt(
                        (blocoZ + (z + 0.5f) / resolucao)
                            / GradeDeTerrenosDoMundo.BlocosPorLado
                            * buracos.GetLength(0)
                    )
                );
                novosBuracos[z, x] = buracos[bz, bx];
            }
            dados.SetHoles(0, 0, novosBuracos);
            return dados;
        }

        private static float Coordenada(int bloco, int indice, int resolucao) =>
            (bloco * (resolucao - 1) + indice)
            / (float)(GradeDeTerrenosDoMundo.BlocosPorLado * (resolucao - 1));

        public static float Amostrar(float[,] valores, float x, float z)
        {
            var px = Mathf.Clamp01(x) * (valores.GetLength(1) - 1);
            var pz = Mathf.Clamp01(z) * (valores.GetLength(0) - 1);
            var x0 = Mathf.FloorToInt(px);
            var z0 = Mathf.FloorToInt(pz);
            var x1 = Mathf.Min(x0 + 1, valores.GetLength(1) - 1);
            var z1 = Mathf.Min(z0 + 1, valores.GetLength(0) - 1);
            return Mathf.Lerp(
                Mathf.Lerp(valores[z0, x0], valores[z0, x1], px - x0),
                Mathf.Lerp(valores[z1, x0], valores[z1, x1], px - x0),
                pz - z0
            );
        }

        private static void CopiarConfiguracao(Terrain origem, Terrain destino)
        {
            destino.materialTemplate = origem.materialTemplate;
            destino.drawInstanced = origem.drawInstanced;
            destino.heightmapPixelError = Mathf.Min(origem.heightmapPixelError, 3f);
            destino.basemapDistance = Mathf.Max(origem.basemapDistance, 500f);
            destino.drawTreesAndFoliage = true;
            destino.detailObjectDistance = origem.detailObjectDistance;
            destino.detailObjectDensity = origem.detailObjectDensity;
            destino.treeDistance = origem.treeDistance;
            destino.treeBillboardDistance = Mathf.Max(
                origem.treeBillboardDistance,
                TerrenoNativoDoMundo.DistanciaDasArvoresEmMalha
            );
            destino.treeCrossFadeLength = origem.treeCrossFadeLength;
            destino.treeMaximumFullLODCount = origem.treeMaximumFullLODCount;
            destino.shadowCastingMode = origem.shadowCastingMode;
            destino.reflectionProbeUsage = origem.reflectionProbeUsage;
        }

        public static void ValidarBordas(Terrain[] terrenos)
        {
            for (var z = 0; z < GradeDeTerrenosDoMundo.BlocosPorLado; z++)
            for (var x = 0; x < GradeDeTerrenosDoMundo.BlocosPorLado; x++)
            {
                var atual = terrenos[z * GradeDeTerrenosDoMundo.BlocosPorLado + x];
                if (x + 1 < GradeDeTerrenosDoMundo.BlocosPorLado)
                    ConferirBorda(
                        atual.terrainData,
                        terrenos[z * GradeDeTerrenosDoMundo.BlocosPorLado + x + 1].terrainData,
                        true
                    );
                if (z + 1 < GradeDeTerrenosDoMundo.BlocosPorLado)
                    ConferirBorda(
                        atual.terrainData,
                        terrenos[(z + 1) * GradeDeTerrenosDoMundo.BlocosPorLado + x].terrainData,
                        false
                    );
            }
        }

        private static void ConferirBorda(TerrainData a, TerrainData b, bool horizontal)
        {
            var n = a.heightmapResolution;
            var alturasA = a.GetHeights(
                horizontal ? n - 1 : 0,
                horizontal ? 0 : n - 1,
                horizontal ? 1 : n,
                horizontal ? n : 1
            );
            var alturasB = b.GetHeights(0, 0, horizontal ? 1 : n, horizontal ? n : 1);
            for (var i = 0; i < n; i++)
                if (
                    Mathf.Abs(
                        alturasA[horizontal ? i : 0, horizontal ? 0 : i]
                            - alturasB[horizontal ? i : 0, horizontal ? 0 : i]
                    ) * a.size.y
                    > ToleranciaDaBorda
                )
                    throw new InvalidOperationException(
                        "Degrau entre blocos: " + a.name + " / " + b.name
                    );
            n = a.alphamapResolution;
            var pinturaA = a.GetAlphamaps(
                horizontal ? n - 1 : 0,
                horizontal ? 0 : n - 1,
                horizontal ? 1 : n,
                horizontal ? n : 1
            );
            var pinturaB = b.GetAlphamaps(0, 0, horizontal ? 1 : n, horizontal ? n : 1);
            for (var i = 0; i < n; i++)
            for (var camada = 0; camada < a.alphamapLayers; camada++)
                if (
                    Mathf.Abs(
                        pinturaA[horizontal ? i : 0, horizontal ? 0 : i, camada]
                            - pinturaB[horizontal ? i : 0, horizontal ? 0 : i, camada]
                    )
                    > 1.01f / 255f
                )
                    throw new InvalidOperationException(
                        "Descontinuidade da pintura: " + a.name + " / " + b.name
                    );
        }
    }
}
