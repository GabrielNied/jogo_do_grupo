using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    /// <summary>Receita nativa reproduzível, sempre em assets/cena próprios, fora do mapa de autoria.</summary>
    public static class DemoDasEstradas
    {
        private const string PastaBase = "Assets/_Game/Features/Mundo/DemoEstradas";

        [MenuItem("Jogo do Grupo/Mundo/Estradas/Criar demonstração isolada")]
        public static void Criar()
        {
            Criar(false);
        }

        private static void Criar(bool limparAoFalhar)
        {
            var original = SceneManager.GetActiveScene();
            var inicialBatch =
                Application.isBatchMode
                && SceneManager.sceneCount == 1
                && string.IsNullOrEmpty(original.path);
            if (
                !inicialBatch
                && Enumerable
                    .Range(0, SceneManager.sceneCount)
                    .Any(i => string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
            )
                throw new InvalidOperationException(
                    "Salve as cenas sem nome antes de criar a demonstração; seu trabalho aberto será preservado."
                );
            var cena = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                inicialBatch ? NewSceneMode.Single : NewSceneMode.Additive
            );
            SceneManager.SetActiveScene(cena);
            var pasta = AssetDatabase.GenerateUniqueAssetPath(PastaBase);
            var ids = new List<string>();
            try
            {
                AplicadorDeEstradas.GarantirPasta(pasta);
                var raiz = new GameObject("Hub");
                var solo = Textura(pasta, "Solo", new Color(0.25f, 0.32f, 0.12f), false);
                var terra = Textura(pasta, "Terra", new Color(0.32f, 0.23f, 0.12f), false);
                var grama = Textura(pasta, "Grama", new Color(0.24f, 0.4f, 0.13f), true);
                var camadaA = Camada(pasta, "Solo", solo);
                var camadaB = Camada(pasta, "Terra", terra);
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(0.19f, 0.3f, 0.09f));
                AssetDatabase.CreateAsset(material, pasta + "/Arvore.mat");
                var modelo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                modelo.name = "Arvore demonstrativa";
                modelo.transform.localScale = new Vector3(1.1f, 2f, 1.1f);
                modelo.GetComponent<Renderer>().sharedMaterial = material;
                var arvore = PrefabUtility.SaveAsPrefabAsset(modelo, pasta + "/Arvore.prefab");
                Object.DestroyImmediate(modelo);
                var terrainMaterial = new Material(
                    Shader.Find("Universal Render Pipeline/Terrain/Lit")
                );
                AssetDatabase.CreateAsset(terrainMaterial, pasta + "/Terrain.mat");
                var esquerda = CriarTerreno(
                    pasta,
                    raiz.transform,
                    0f,
                    camadaA,
                    camadaB,
                    grama,
                    arvore,
                    terrainMaterial
                );
                var direita = CriarTerreno(
                    pasta,
                    raiz.transform,
                    32f,
                    camadaA,
                    camadaB,
                    grama,
                    arvore,
                    terrainMaterial
                );
                esquerda.SetNeighbors(null, null, direita, null);
                direita.SetNeighbors(esquerda, null, null, null);
                var luz = new GameObject("Sol").AddComponent<Light>();
                luz.transform.SetParent(raiz.transform, false);
                luz.type = LightType.Directional;
                luz.intensity = 2f;
                luz.shadows = LightShadows.Soft;
                luz.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.4f, 0.43f, 0.46f);
                var estradaTerra = CriarEstrada(
                    raiz.transform,
                    "Terra - caminho de cavalos",
                    10f,
                    TipoDeEstrada.Terra
                );
                var estradaLajotas = CriarEstrada(
                    raiz.transform,
                    "Lajotas - caminho da vila",
                    24f,
                    TipoDeEstrada.Lajotas
                );
                ids.Add(estradaTerra.Id);
                ids.Add(estradaLajotas.Id);
                EditorSceneManager.SaveScene(cena, pasta + "/DemoEstradas.unity");
                AplicadorDeEstradas.Aplicar(estradaTerra);
                AplicadorDeEstradas.Aplicar(estradaLajotas);
                PrefabUtility.SaveAsPrefabAsset(raiz, pasta + "/MapaDemo.prefab");
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(cena);
                VisualDaEstrada.CapturarDemo(
                    raiz,
                    Path.Combine(Application.dataPath, "../Library/ValidacaoEstradas.png"),
                    (estrada, ponto) =>
                        AplicadorDeEstradas.AlturaEm(AplicadorDeEstradas.TerrenosDa(estrada), ponto)
                );
                Selection.activeGameObject = estradaTerra.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log(
                    "[Estradas] Demonstração isolada criada em " + pasta + "/DemoEstradas.unity"
                );
            }
            catch
            {
                if (original.IsValid())
                    SceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(cena, true);
                if (limparAoFalhar)
                    LimparAssetsDestaValidacao(pasta, ids);
                throw;
            }
        }

        /// <summary>Executável por unity:executar; não publica nem abre o mapa do Gabriel.</summary>
        public static void Validar()
        {
            var original = SceneManager.GetActiveScene();
            Criar(true);
            var cena = SceneManager.GetActiveScene();
            // Unity não descarrega a última cena; uma sentinela vazia permite testar reload real.
            var sentinela = default(Scene);
            if (SceneManager.sceneCount == 1)
            {
                sentinela = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive
                );
                SceneManager.SetActiveScene(cena);
            }
            var caminho = cena.path;
            var pastaAtual = Path.GetDirectoryName(caminho).Replace('\\', '/');
            var idsAtuais = cena.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<EstradaNativa>())
                .Select(e => e.Id)
                .ToArray();
            var aprovada = false;
            try
            {
                Verificar(
                    cena != original && caminho.Contains("DemoEstradas"),
                    "A validação precisa de uma cena própria."
                );
                var raiz = cena.GetRootGameObjects().Single(o => o.name == "Hub");
                var estradas = raiz.GetComponentsInChildren<EstradaNativa>();
                var terra = estradas.Single(e => e.Tipo == TipoDeEstrada.Terra);
                var lajotas = estradas.Single(e => e.Tipo == TipoDeEstrada.Lajotas);
                var terrenos = raiz.GetComponentsInChildren<Terrain>()
                    .OrderBy(t => t.transform.position.x)
                    .ToArray();
                var registro = (RegistroDaEstrada)terra.Registro;
                Verificar(
                    registro.Aplicada && registro.Alteracoes.Count == 2,
                    "A estrada precisa atravessar dois blocos."
                );
                Verificar(
                    terrenos.All(t => t.heightmapPixelError <= 0.25f),
                    "A precisão visual do Terrain não foi aplicada."
                );
                foreach (var alteracao in registro.Alteracoes)
                {
                    Verificar(
                        alteracao.ArvoresRemovidas.Length > 0,
                        "Árvores nativas não foram removidas."
                    );
                    Verificar(
                        alteracao.Detalhes.Count == 2,
                        "Todas as camadas de detalhes devem ser tratadas."
                    );
                    Verificar(
                        alteracao.Detalhes.All(c => c.Antes.Any(v => v > 0)),
                        "O teste deve ter grama nas duas camadas."
                    );
                    var primeiro = alteracao.IndicesAltura.Length / 2;
                    Verificar(
                        alteracao.AlturasAntes[primeiro] > alteracao.AlturasDepois[primeiro],
                        "O caminho deve ser rebaixado."
                    );
                }
                var centro = new Vector3(32f, 0f, 12f);
                var altura = AplicadorDeEstradas.AlturaEm(terrenos, centro);
                AplicadorDeEstradas.Aplicar(terra);
                Verificar(
                    Mathf.Abs(AplicadorDeEstradas.AlturaEm(terrenos, centro) - altura) < 0.002f,
                    "Reaplicar acumulou o rebaixo."
                );
                var amostraEsquerda = terrenos[0].terrainData.GetHeights(128, 0, 1, 129);
                var amostraDireita = terrenos[1].terrainData.GetHeights(0, 0, 1, 129);
                for (var z = 0; z < 129; z++)
                    Verificar(
                        Mathf.Abs(amostraEsquerda[z, 0] - amostraDireita[z, 0]) < 0.0001f,
                        "A borda dos blocos criou uma emenda no relevo."
                    );
                ValidarConflitos(terra, terrenos[0]);
                ValidarEdicaoDoTracado(terra);
                // Acrescenta conteúdo fora do corredor; restaurar não pode substituí-lo.
                var dados = terrenos[0].terrainData;
                var externas = dados.treeInstances.ToList();
                externas.Add(Arvore(new Vector3(0.1f, 0f, 0.94f)));
                dados.SetTreeInstances(externas.ToArray(), false);
                var pintura = new float[1, 1, 2];
                pintura[0, 0, 1] = 1f;
                dados.SetAlphamaps(0, 0, pintura);
                var detalhesExternos = new int[1, 1];
                detalhesExternos[0, 0] = 7;
                dados.SetDetailLayer(0, 0, 0, detalhesExternos);
                var alturasExternas = dados.GetHeights(0, 0, 1, 1);
                alturasExternas[0, 0] += 0.02f;
                dados.SetHeights(0, 0, alturasExternas);
                EditorUtility.SetDirty(dados);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(cena);
                var caminhoRegistro = AssetDatabase.GetAssetPath(terra.Registro);
                var jsonRegistro = EditorJsonUtility.ToJson(terra.Registro);
                EditorSceneManager.CloseScene(cena, true);
                // Fecha o asset também: reabrir somente a cena pode reutilizar o cache em RAM.
                Resources.UnloadAsset(registro);
                var registroDoDisco = AssetDatabase.LoadAssetAtPath<RegistroDaEstrada>(
                    caminhoRegistro
                );
                Verificar(
                    EditorJsonUtility.ToJson(registroDoDisco) == jsonRegistro,
                    "O backup perdeu dados ao ser descarregado e lido do disco."
                );
                cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(cena);
                raiz = cena.GetRootGameObjects().Single(o => o.name == "Hub");
                terra = raiz.GetComponentsInChildren<EstradaNativa>()
                    .Single(e => e.Tipo == TipoDeEstrada.Terra);
                lajotas = raiz.GetComponentsInChildren<EstradaNativa>()
                    .Single(e => e.Tipo == TipoDeEstrada.Lajotas);
                terrenos = raiz.GetComponentsInChildren<Terrain>()
                    .OrderBy(t => t.transform.position.x)
                    .ToArray();
                dados = terrenos[0].terrainData;
                Verificar(
                    terra.Registro is RegistroDaEstrada salvo && salvo.Aplicada,
                    "O backup não sobreviveu a salvar/reabrir."
                );
                foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
                    Verificar(
                        filtro.sharedMesh != null && AssetDatabase.Contains(filtro.sharedMesh),
                        "Uma malha publicada não é persistente."
                    );
                var quantidadeAntes = dados.treeInstanceCount;
                var removidas = ((RegistroDaEstrada)terra.Registro)
                    .Alteracoes.Single(a => a.Dados == dados)
                    .ArvoresRemovidas.Length;
                AplicadorDeEstradas.Restaurar(terra);
                Verificar(
                    dados.treeInstanceCount == quantidadeAntes + removidas,
                    "Restauração perdeu árvores externas ou não repôs as removidas."
                );
                Verificar(
                    dados.treeInstances.Any(a => a.position.x == 0.1f && a.position.z == 0.94f),
                    "Árvore criada fora da estrada foi perdida."
                );
                Verificar(
                    dados.GetDetailLayer(0, 0, 1, 1, 0)[0, 0] == 7,
                    "Restauração apagou detalhes fora da faixa."
                );
                Verificar(
                    dados.GetAlphamaps(0, 0, 1, 1)[0, 0, 1] > 0.99f,
                    "Restauração apagou pintura externa."
                );
                Verificar(
                    Mathf.Abs(dados.GetHeights(0, 0, 1, 1)[0, 0] - alturasExternas[0, 0]) < 0.0001f,
                    "Restauração apagou relevo externo."
                );
                Verificar(
                    terrenos.All(t => t.heightmapPixelError <= 0.25f),
                    "Restaurar uma estrada prejudicou outra no mesmo bloco."
                );
                AplicadorDeEstradas.Restaurar(lajotas);
                Verificar(
                    terrenos.All(t => Mathf.Approximately(t.heightmapPixelError, 5f)),
                    "A última restauração não repôs a precisão original do Terrain."
                );
                AplicadorDeEstradas.Aplicar(terra);
                AplicadorDeEstradas.Aplicar(lajotas);
                PrefabUtility.SaveAsPrefabAsset(
                    raiz,
                    Path.GetDirectoryName(caminho).Replace('\\', '/') + "/MapaDemo.prefab"
                );
                Verificar(
                    !raiz.GetComponentsInChildren<MeshCollider>().Any(),
                    "A estrada não deve criar colisão paralela ao Terrain."
                );
                Debug.Log(
                    "[Estradas] VALIDAÇÃO APROVADA: rebaixo, dois blocos sem emenda, todas as camadas, árvores, reaplicação idempotente, conflitos, conteúdo externo, persistência e prefab."
                );
                aprovada = true;
            }
            finally
            {
                if (original.IsValid() && original.isLoaded)
                    SceneManager.SetActiveScene(original);
                else if (sentinela.IsValid() && sentinela.isLoaded)
                    SceneManager.SetActiveScene(sentinela);
                if (cena.IsValid() && cena.isLoaded)
                    EditorSceneManager.CloseScene(cena, true);
                if (sentinela.IsValid() && sentinela.isLoaded && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(sentinela, true);
                if (!aprovada)
                    LimparAssetsDestaValidacao(pastaAtual, idsAtuais);
            }
        }

        private static void LimparAssetsDestaValidacao(string pasta, IEnumerable<string> ids)
        {
            // Apenas caminhos únicos produzidos e mantidos em memória por esta execução.
            foreach (var id in ids)
                if (Guid.TryParseExact(id, "N", out _))
                    AssetDatabase.DeleteAsset(AplicadorDeEstradas.PastaGerada + "/" + id);
            if (pasta.StartsWith(PastaBase, StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(pasta);
            AssetDatabase.SaveAssets();
            Debug.Log("[Estradas] Assets da validação que falhou foram removidos: " + pasta);
        }

        private static void ValidarConflitos(EstradaNativa estrada, Terrain terreno)
        {
            var registro = (RegistroDaEstrada)estrada.Registro;
            var alteracao = registro.Alteracoes.Single(a => a.Dados == terreno.terrainData);
            var i = alteracao.IndicesAltura.Length / 2;
            var indice = alteracao.IndicesAltura[i];
            var x = indice % alteracao.ResolucaoAltura;
            var z = indice / alteracao.ResolucaoAltura;
            var original = terreno.terrainData.GetHeights(x, z, 1, 1);
            var modificado = new float[1, 1];
            modificado[0, 0] = original[0, 0] + 0.01f;
            terreno.terrainData.SetHeights(x, z, modificado);
            VerificarRecusa(
                () => AplicadorDeEstradas.Restaurar(estrada),
                "Edição manual dentro da faixa deve interromper a restauração."
            );
            terreno.terrainData.SetHeights(x, z, original);
            var prototipos = terreno.terrainData.detailPrototypes;
            terreno.terrainData.detailPrototypes = prototipos.Reverse().ToArray();
            VerificarRecusa(
                () => AplicadorDeEstradas.Restaurar(estrada),
                "Reordenar protótipos precisa ser detectado."
            );
            terreno.terrainData.detailPrototypes = prototipos;
            AplicadorDeEstradas.Aplicar(estrada);
            var copia = Object.Instantiate(estrada.gameObject, estrada.transform.parent);
            copia.name = "Cópia indevida - teste de proteção";
            try
            {
                VerificarRecusa(
                    () => AplicadorDeEstradas.Restaurar(copia.GetComponent<EstradaNativa>()),
                    "Uma cópia não pode restaurar o terreno da estrada original."
                );
                Verificar(
                    ((RegistroDaEstrada)estrada.Registro).Aplicada,
                    "A cópia alterou o registro original."
                );
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        private static void ValidarEdicaoDoTracado(EstradaNativa estrada)
        {
            var posicao = estrada.transform.position;
            var antigas = ((RegistroDaEstrada)estrada.Registro).Alteracoes;
            estrada.transform.position += Vector3.forward * 3f;
            AplicadorDeEstradas.Aplicar(estrada);
            var novas = ((RegistroDaEstrada)estrada.Registro).Alteracoes;
            foreach (var antiga in antigas)
            {
                var nova = novas.Single(a => a.Dados == antiga.Dados);
                var indicesNovos = new System.Collections.Generic.HashSet<int>(nova.IndicesAltura);
                var indiceAntigo = Array.FindIndex(
                    antiga.IndicesAltura,
                    i => !indicesNovos.Contains(i)
                );
                Verificar(
                    indiceAntigo >= 0,
                    "O teste precisa abandonar uma parte do traçado anterior."
                );
                var indice = antiga.IndicesAltura[indiceAntigo];
                var altura = antiga.Dados.GetHeights(
                    indice % antiga.ResolucaoAltura,
                    indice / antiga.ResolucaoAltura,
                    1,
                    1
                )[0, 0];
                Verificar(
                    Mathf.Abs(altura - antiga.AlturasAntes[indiceAntigo]) < 0.00001f,
                    "Mover e reaplicar deixou rebaixo no traçado antigo."
                );
            }
            estrada.transform.position = posicao;
            AplicadorDeEstradas.Aplicar(estrada);
        }

        private static Terrain CriarTerreno(
            string pasta,
            Transform pai,
            float origemX,
            TerrainLayer camadaA,
            TerrainLayer camadaB,
            Texture2D grama,
            GameObject arvore,
            Material material
        )
        {
            var dados = new TerrainData
            {
                name = "Bloco" + origemX,
                heightmapResolution = 129,
                size = new Vector3(32f, 10f, 32f),
                alphamapResolution = 64,
            };
            var alturas = new float[129, 129];
            for (var z = 0; z < 129; z++)
            for (var x = 0; x < 129; x++)
                alturas[z, x] =
                    0.3f
                    + Mathf.PerlinNoise((origemX + x * 0.25f) * 0.07f, z * 0.25f * 0.07f) * 0.03f;
            dados.SetHeights(0, 0, alturas);
            dados.terrainLayers = new[] { camadaA, camadaB };
            var pintura = new float[64, 64, 2];
            for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                pintura[z, x, 0] = 1f;
            dados.SetAlphamaps(0, 0, pintura);
            dados.SetDetailResolution(64, 8);
            dados.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = grama,
                    renderMode = DetailRenderMode.GrassBillboard,
                    minWidth = 0.35f,
                    maxWidth = 0.65f,
                    minHeight = 0.2f,
                    maxHeight = 0.4f,
                    healthyColor = Color.white,
                    dryColor = Color.white,
                },
                new DetailPrototype
                {
                    prototypeTexture = camadaA.diffuseTexture,
                    renderMode = DetailRenderMode.GrassBillboard,
                    minWidth = 0.15f,
                    maxWidth = 0.3f,
                    minHeight = 0.1f,
                    maxHeight = 0.2f,
                    healthyColor = new Color(0.7f, 0.8f, 0.5f),
                    dryColor = new Color(0.7f, 0.8f, 0.5f),
                },
            };
            var densidade = new int[64, 64];
            for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                densidade[z, x] = 1;
            dados.SetDetailLayer(0, 0, 0, densidade);
            dados.SetDetailLayer(0, 0, 1, densidade);
            dados.treePrototypes = new[] { new TreePrototype { prefab = arvore } };
            dados.SetTreeInstances(
                new[]
                {
                    Arvore(new Vector3(0.35f, 0f, 0.35f)),
                    Arvore(new Vector3(0.7f, 0f, 0.75f)),
                    Arvore(new Vector3(0.45f, 0f, 0.12f)),
                    Arvore(new Vector3(0.8f, 0f, 0.94f)),
                },
                true
            );
            AssetDatabase.CreateAsset(dados, pasta + "/Bloco" + origemX + ".asset");
            var objeto = Terrain.CreateTerrainGameObject(dados);
            objeto.name = "Terrain " + origemX;
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(origemX, 0f, 0f);
            var terreno = objeto.GetComponent<Terrain>();
            terreno.heightmapPixelError = 5f;
            terreno.materialTemplate = material;
            terreno.detailObjectDistance = 100f;
            terreno.treeDistance = 150f;
            return terreno;
        }

        private static EstradaNativa CriarEstrada(
            Transform pai,
            string nome,
            float z,
            TipoDeEstrada tipo
        )
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(pai, false);
            var spline = objeto.AddComponent<SplineContainer>();
            spline.Spline = new Spline();
            spline.Spline.Add(new BezierKnot(new float3(4f, 3f, z)), TangentMode.AutoSmooth);
            spline.Spline.Add(new BezierKnot(new float3(30f, 3f, z + 2f)), TangentMode.AutoSmooth);
            spline.Spline.Add(new BezierKnot(new float3(60f, 3f, z - 1f)), TangentMode.AutoSmooth);
            var estrada = objeto.AddComponent<EstradaNativa>();
            estrada.Id = Guid.NewGuid().ToString("N");
            var serializado = new SerializedObject(estrada);
            serializado.FindProperty("_tipo").enumValueIndex = (int)tipo;
            serializado.ApplyModifiedPropertiesWithoutUndo();
            return estrada;
        }

        private static Texture2D Textura(string pasta, string nome, Color cor, bool transparente)
        {
            const int tamanho = 32;
            var textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, true)
            {
                name = nome,
                wrapMode = TextureWrapMode.Repeat,
            };
            var pixels = new Color[tamanho * tamanho];
            for (var y = 0; y < tamanho; y++)
            for (var x = 0; x < tamanho; x++)
            {
                var c = cor * (0.85f + Mathf.PerlinNoise(x * 0.8f, y * 0.8f) * 0.3f);
                c.a = transparente
                    ? (
                        Mathf.Abs(x - tamanho * 0.5f + Mathf.Sin(y * 0.3f) * 2f)
                        < (1f - y / (float)tamanho) * 6f
                            ? 1f
                            : 0f
                    )
                    : 1f;
                pixels[y * tamanho + x] = c;
            }
            textura.SetPixels(pixels);
            textura.Apply();
            AssetDatabase.CreateAsset(textura, pasta + "/" + nome + ".asset");
            return textura;
        }

        private static TerrainLayer Camada(string pasta, string nome, Texture2D textura)
        {
            var camada = new TerrainLayer
            {
                name = nome,
                diffuseTexture = textura,
                tileSize = new Vector2(2f, 2f),
            };
            AssetDatabase.CreateAsset(camada, pasta + "/" + nome + ".terrainlayer");
            return camada;
        }

        private static TreeInstance Arvore(Vector3 posicao)
        {
            return new TreeInstance
            {
                position = posicao,
                prototypeIndex = 0,
                widthScale = 1f,
                heightScale = 1f,
                color = Color.white,
                lightmapColor = Color.white,
            };
        }

        private static void VerificarRecusa(Action acao, string mensagem)
        {
            try
            {
                acao();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(mensagem);
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException("[Estradas] " + mensagem);
        }
    }
}
