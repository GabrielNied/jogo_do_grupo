using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Adapta a cópia licenciada local, sem distribuir o conteúdo da Asset Store.</summary>
    public static class AdaptadorFantasyLandscapeUrp
    {
        private const string PastaImportada = "Assets/FantasyEnvironments";
        private const string PastaDoPack = "Assets/ThirdParty/Pxltiger/FantasyLandscape";
        private const string ShaderLit = "Universal Render Pipeline/Lit";
        private const string ShaderParticulas = "Universal Render Pipeline/Particles/Unlit";

        // Uma malha 3D até ocupar menos de 2% da altura da tela; depois é ocultada.
        // Não são geradas malhas simplificadas nem usado o billboard Tree Creator antigo.
        private const float AlturaRelativaDeCorte = 0.02f;

        [MenuItem("Jogo do Grupo/Mundo/Adaptar Fantasy Landscape para URP")]
        public static void Converter()
        {
            var lit = Shader.Find(ShaderLit);
            var particulas = Shader.Find(ShaderParticulas);
            if (lit == null || particulas == null || !lit.isSupported || !particulas.isSupported)
            {
                throw new InvalidOperationException(
                    "Os shaders URP do projeto não estão disponíveis."
                );
            }

            OrganizarPack();
            var convertidos = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { PastaDoPack }))
            {
                var caminho = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
                if (
                    material.shader != null
                    && material.shader.name.StartsWith(
                        "Universal Render Pipeline/",
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                var shaderAntigo = material.shader != null ? material.shader.name : "";
                var folha =
                    shaderAntigo.Contains("Leaves")
                    || material.name.ToLowerInvariant().Contains("leaves");
                var efeito = caminho.Contains("/FX/");
                var modo = Ler(material, "_Mode", 0f);
                var recorte =
                    folha
                    || (!efeito && (modo == 1f || material.IsKeywordEnabled("_ALPHATEST_ON")));
                var transparente = efeito || (!recorte && modo >= 2f);
                var textura = material.HasProperty("_MainTex")
                    ? material.GetTexture("_MainTex")
                    : null;
                var escala = material.HasProperty("_MainTex")
                    ? material.GetTextureScale("_MainTex")
                    : Vector2.one;
                var deslocamento = material.HasProperty("_MainTex")
                    ? material.GetTextureOffset("_MainTex")
                    : Vector2.zero;
                var cor = LerCor(material, "_Color", Color.white);
                var cutoff = Ler(material, "_Cutoff", 0.5f);
                var brilho = Ler(material, "_Glossiness", 0f);
                var metal = Ler(material, "_Metallic", 0f);
                var emissao = LerCor(material, "_EmissionColor", Color.black);
                var normal = material.HasProperty("_BumpMap")
                    ? material.GetTexture("_BumpMap")
                    : null;
                var mapaMetal = material.HasProperty("_MetallicGlossMap")
                    ? material.GetTexture("_MetallicGlossMap")
                    : null;
                var mapaEmissao = material.HasProperty("_EmissionMap")
                    ? material.GetTexture("_EmissionMap")
                    : null;
                var normalEscala = Ler(material, "_BumpScale", 1f);

                Undo.RecordObject(material, "Adaptar Fantasy Landscape para URP");
                material.shader = efeito ? particulas : lit;
                material.shaderKeywords = Array.Empty<string>();
                material.SetTexture("_BaseMap", textura);
                material.SetTextureScale("_BaseMap", escala);
                material.SetTextureOffset("_BaseMap", deslocamento);
                material.SetColor("_BaseColor", cor);
                material.SetFloat("_Surface", transparente ? 1f : 0f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", recorte ? 1f : 0f);
                material.SetFloat("_Cutoff", cutoff);
                material.SetFloat(
                    "_Cull",
                    recorte || efeito ? (float)CullMode.Off : (float)CullMode.Back
                );
                material.SetFloat("_ZWrite", transparente ? 0f : 1f);
                material.SetFloat(
                    "_SrcBlend",
                    transparente ? (float)BlendMode.SrcAlpha : (float)BlendMode.One
                );
                material.SetFloat(
                    "_DstBlend",
                    transparente ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero
                );
                if (!efeito)
                {
                    material.SetFloat("_Smoothness", brilho);
                    material.SetFloat("_Metallic", metal);
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", normalEscala);
                    material.SetTexture("_MetallicGlossMap", mapaMetal);
                    material.SetTexture("_EmissionMap", mapaEmissao);
                    material.SetColor("_EmissionColor", emissao);
                    DefinirKeyword(material, "_NORMALMAP", normal != null);
                    DefinirKeyword(material, "_METALLICSPECGLOSSMAP", mapaMetal != null);
                    DefinirKeyword(material, "_EMISSION", emissao.maxColorComponent > 0f);
                }
                DefinirKeyword(material, "_ALPHATEST_ON", recorte);
                DefinirKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparente);
                material.SetOverrideTag(
                    "RenderType",
                    transparente ? "Transparent"
                        : recorte ? "TransparentCutout"
                        : "Opaque"
                );
                material.renderQueue =
                    transparente ? (int)RenderQueue.Transparent
                    : recorte ? (int)RenderQueue.AlphaTest
                    : (int)RenderQueue.Geometry;
                material.SetShaderPassEnabled("ShadowCaster", !transparente);
                material.enableInstancing = true;
                material.doubleSidedGI = recorte;
                EditorUtility.SetDirty(material);
                convertidos++;
            }

            AdaptarLodDasArvores();
            AssetDatabase.SaveAssets();
            foreach (var terreno in Terrain.activeTerrains)
            {
                terreno.Flush();
            }
            Validar();
            Debug.Log(
                $"[Mundo] Fantasy Landscape: {convertidos} materiais convertidos. GUIDs e pintura preservados; vegetação estática, sem o vento Tree Creator antigo."
            );
        }

        private static void OrganizarPack()
        {
            if (AssetDatabase.IsValidFolder(PastaImportada))
            {
                if (AssetDatabase.IsValidFolder(PastaDoPack))
                {
                    throw new InvalidOperationException(
                        "Há duas cópias do Fantasy Landscape. Não serão mescladas automaticamente."
                    );
                }
                if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/Pxltiger"))
                {
                    AssetDatabase.CreateFolder("Assets/ThirdParty", "Pxltiger");
                }
                var erro = AssetDatabase.MoveAsset(PastaImportada, PastaDoPack);
                if (!string.IsNullOrEmpty(erro))
                    throw new InvalidOperationException(erro);
            }
            if (!AssetDatabase.IsValidFolder(PastaDoPack))
            {
                throw new InvalidOperationException(
                    "Importe Fantasy Landscape 103573 pelo Package Manager antes de adaptar."
                );
            }
        }

        public static void Validar()
        {
            var quantidade = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { PastaDoPack }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (
                    material.shader == null
                    || !material.shader.isSupported
                    || !material.shader.name.StartsWith(
                        "Universal Render Pipeline/",
                        StringComparison.Ordinal
                    )
                    || ShaderUtil.ShaderHasError(material.shader)
                )
                {
                    throw new InvalidOperationException("Material incompatível: " + material.name);
                }
                if (
                    material.name.ToLowerInvariant().Contains("leaves")
                    && (
                        material.GetFloat("_AlphaClip") != 1f
                        || material.GetFloat("_Cull") != 0f
                        || material.GetTexture("_BaseMap") == null
                    )
                )
                {
                    throw new InvalidOperationException(
                        "Folhagem sem textura, recorte ou duas faces: " + material.name
                    );
                }
                quantidade++;
            }
            if (quantidade == 0)
                throw new InvalidOperationException("Nenhum material do pack foi encontrado.");
            Debug.Log($"[Mundo] {quantidade} materiais URP validados.");
            foreach (var caminho in CaminhosDasArvores())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                var grupo = prefab.GetComponent<LODGroup>();
                if (grupo == null || grupo.GetLODs().Length == 0)
                {
                    throw new InvalidOperationException("Árvore sem LOD Group na raiz: " + caminho);
                }
            }
        }

        private static string[] CaminhosDasArvores()
        {
            var pasta = PastaDoPack + "/Environments/Ambient-Occlusion-Trees/Prefabs";
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { pasta });
            return Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath);
        }

        private static void AdaptarLodDasArvores()
        {
            var adaptadas = 0;
            foreach (var caminho in CaminhosDasArvores())
            {
                var raiz = PrefabUtility.LoadPrefabContents(caminho);
                try
                {
                    // Respeitar LODs que o artista tenha configurado depois da adaptação.
                    if (raiz.GetComponent<LODGroup>() != null)
                        continue;
                    var renderers = raiz.GetComponentsInChildren<MeshRenderer>(true);
                    if (renderers.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Árvore sem malha para LOD: " + caminho
                        );
                    }
                    var grupo = raiz.AddComponent<LODGroup>();
                    grupo.fadeMode = LODFadeMode.None;
                    grupo.SetLODs(new[] { new LOD(AlturaRelativaDeCorte, renderers) });
                    grupo.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                    adaptadas++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            Debug.Log(
                $"[Mundo] {adaptadas} árvores adaptadas com LOD Group na raiz, sem billboard legado e com suporte à rotação do Terrain."
            );
        }

        public static void CapturarPrevia()
        {
            Validar();
            var previa = new PreviewRenderUtility();
            Texture2D captura = null;
            var anterior = RenderTexture.active;
            try
            {
                var nomes = new[] { "Oak_tree1", "Birch_tree1", "Willow_tree1" };
                var limites = new Bounds();
                var iniciou = false;
                for (var i = 0; i < nomes.Length; i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        PastaDoPack
                            + "/Environments/Ambient-Occlusion-Trees/Prefabs/"
                            + nomes[i]
                            + ".prefab"
                    );
                    if (prefab == null)
                        throw new InvalidOperationException("Árvore não encontrada: " + nomes[i]);
                    var arvore = Object.Instantiate(
                        prefab,
                        new Vector3(i * 13f, 0f, 0f),
                        Quaternion.identity
                    );
                    previa.AddSingleGO(arvore);
                    foreach (var renderer in arvore.GetComponentsInChildren<Renderer>())
                    {
                        if (!iniciou)
                        {
                            limites = renderer.bounds;
                            iniciou = true;
                        }
                        else
                            limites.Encapsulate(renderer.bounds);
                    }
                }
                previa.lights[0].intensity = 1.7f;
                previa.lights[0].transform.rotation = Quaternion.Euler(40f, -35f, 0f);
                previa.lights[1].intensity = 0.8f;
                previa.lights[1].transform.rotation = Quaternion.Euler(15f, 145f, 0f);
                previa.ambientColor = new Color(0.45f, 0.45f, 0.45f);
                var camera = previa.camera;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 1000f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.45f, 0.55f);
                camera.transform.position =
                    limites.center
                    + new Vector3(
                        0f,
                        limites.size.y * 0.15f,
                        -Mathf.Max(limites.size.x, limites.size.y) * 1.4f
                    );
                camera.transform.LookAt(limites.center);
                previa.BeginPreview(new Rect(0, 0, 1600, 900), GUIStyle.none);
                previa.Render(true);
                RenderTexture.active = (RenderTexture)previa.EndPreview();
                captura = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                captura.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                captura.Apply();
                Directory.CreateDirectory("prints");
                File.WriteAllBytes("prints/fantasy-landscape-urp.png", captura.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = anterior;
                if (captura != null)
                    Object.DestroyImmediate(captura);
                previa.Cleanup();
            }
        }

        private static float Ler(Material material, string nome, float padrao) =>
            material.HasProperty(nome) ? material.GetFloat(nome) : padrao;

        private static Color LerCor(Material material, string nome, Color padrao) =>
            material.HasProperty(nome) ? material.GetColor(nome) : padrao;

        private static void DefinirKeyword(Material material, string nome, bool ativo)
        {
            if (ativo)
                material.EnableKeyword(nome);
            else
                material.DisableKeyword(nome);
        }
    }
}
