using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor
{
    /// <summary>Importacao e cena isoladas; nao substitui os jogadores das racas.</summary>
    public static class TesteHumanMaleHumanoid
    {
        const string Pasta = "Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest";
        const string Modelo = Pasta + "/Human_Male_Base_v1_UnityTest.fbx";
        const string Cena = Pasta + "/HumanMaleHumanoid.unity";
        const string Prefab = Pasta + "/HumanMaleHumanoid.prefab";
        const string MaterialPele = Pasta + "/HumanMaleURP.mat";
        const string Controller = Pasta + "/HumanMaleHumanoid.controller";
        const float AlturaEsperada = 1.864161f;
        const int LarguraCaptura = 1280;
        const int AlturaCaptura = 1440;

        [Serializable]
        sealed class Relatorio
        {
            public string cena = Cena;
            public string prefab = Prefab;
            public string hashFbx;
            public bool avatarAntesDoMapeamentoValido;
            public bool avatarValido;
            public bool avatarHumanoid;
            public bool mapeamentoManualAplicadoNestaExecucao;
            public bool tPoseConferida;
            public bool optimizeGameObjects;
            public float alturaMetros;
            public float pesNoChao;
            public int verticesImportados;
            public int maximoInfluencias;
            public int verticesSemPeso;
            public bool weaponRAcessivel;
            public bool costasAcessivel;
            public int clipesEncontrados;
            public int clipesHumanoid;
            public string animacao;
            public bool animacaoVerificada;
            public float deslocamentoAnimadoMaximo;
            public string status;
            public List<string> mapeamento = new List<string>();
            public List<string> inventario = new List<string>();
            public List<string> mensagens = new List<string>();
        }

        static readonly Dictionary<HumanBodyBones, string> Mapa = CriarMapa();

        static Dictionary<HumanBodyBones, string> CriarMapa()
        {
            return new Dictionary<HumanBodyBones, string>
            {
                { HumanBodyBones.Hips, "Quadril" },
                { HumanBodyBones.Spine, "Coluna" },
                { HumanBodyBones.Chest, "Tronco" },
                { HumanBodyBones.Neck, "Pescoco" },
                { HumanBodyBones.Head, "Cabeca" },
                { HumanBodyBones.LeftShoulder, "Clavicula_E" },
                { HumanBodyBones.LeftUpperArm, "Braco_E" },
                { HumanBodyBones.LeftLowerArm, "Antebraco_E" },
                { HumanBodyBones.LeftHand, "Mao_E" },
                { HumanBodyBones.LeftUpperLeg, "Perna_E" },
                { HumanBodyBones.LeftLowerLeg, "Canela_E" },
                { HumanBodyBones.LeftFoot, "Pe_E" },
                { HumanBodyBones.LeftToes, "DedosPe_E" },
                { HumanBodyBones.RightShoulder, "Clavicula_D" },
                { HumanBodyBones.RightUpperArm, "Braco_D" },
                { HumanBodyBones.RightLowerArm, "Antebraco_D" },
                { HumanBodyBones.RightHand, "Mao_D" },
                { HumanBodyBones.RightUpperLeg, "Perna_D" },
                { HumanBodyBones.RightLowerLeg, "Canela_D" },
                { HumanBodyBones.RightFoot, "Pe_D" },
                { HumanBodyBones.RightToes, "DedosPe_D" },
            };
        }

        static string Saida =>
            Environment.GetEnvironmentVariable("HUMAN_UNITY_TEST_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "human-male-unity03");

        [MenuItem("Jogo do Grupo/Personagem/Testar Human Male Humanoid")]
        public static void Gerar()
        {
            Directory.CreateDirectory(Saida);
            var relatorio = new Relatorio();
            Application.LogCallback registrar = (mensagem, stack, tipo) =>
            {
                if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Warning)
                    relatorio.mensagens.Add(tipo + ": " + mensagem);
            };
            Application.logMessageReceived += registrar;
            try
            {
                GerarConteudo(relatorio);
            }
            catch (Exception erro)
            {
                relatorio.status = "Erro: " + erro.Message;
                throw;
            }
            finally
            {
                Application.logMessageReceived -= registrar;
                File.WriteAllText(
                    Path.Combine(Saida, "validacao-unity03.json"),
                    JsonUtility.ToJson(relatorio, true)
                );
                Debug.Log("[HumanMaleHumanoid] " + relatorio.status + " | Evidencia: " + Saida);
            }
        }

        static void GerarConteudo(Relatorio relatorio)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            using (var sha = SHA256.Create())
                relatorio.hashFbx = BitConverter
                    .ToString(sha.ComputeHash(File.ReadAllBytes(Modelo)))
                    .Replace("-", "")
                    .ToLowerInvariant();

            var importer = (ModelImporter)AssetImporter.GetAtPath(Modelo);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = true;
            importer.skinWeights = ModelImporterSkinWeights.Standard;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
            var avatar = modelo.GetComponent<Animator>()?.avatar;
            // Na primeira importacao e a tentativa automatica. Depois pode ser o mapa persistido.
            relatorio.avatarAntesDoMapeamentoValido =
                avatar != null && avatar.isValid && avatar.isHuman;

            // Mantem o FBX intacto: a correspondencia vive somente no importador.
            var descricao = importer.humanDescription;
            bool mapaCorreto =
                descricao.human != null
                && Mapa.All(par =>
                    descricao.human.Any(h =>
                        h.humanName == par.Key.ToString() && h.boneName == par.Value
                    )
                );
            if (!relatorio.avatarAntesDoMapeamentoValido || !mapaCorreto)
            {
                descricao.human = Mapa.Select(par => new HumanBone
                    {
                        humanName = par.Key.ToString(),
                        boneName = par.Value,
                        limit = new HumanLimit { useDefaultValues = true },
                    })
                    .ToArray();
                descricao.skeleton = modelo
                    .GetComponentsInChildren<Transform>(true)
                    .Select(t => new SkeletonBone
                    {
                        name = t.name,
                        position = t.localPosition,
                        rotation = t.localRotation,
                        scale = t.localScale,
                    })
                    .ToArray();
                descricao.upperArmTwist = descricao.lowerArmTwist = .5f;
                descricao.upperLegTwist = descricao.lowerLegTwist = .5f;
                descricao.armStretch = descricao.legStretch = .05f;
                importer.humanDescription = descricao;
                importer.SaveAndReimport();
                modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
                avatar = modelo.GetComponent<Animator>()?.avatar;
                relatorio.mapeamentoManualAplicadoNestaExecucao = true;
            }
            relatorio.avatarValido = avatar != null && avatar.isValid;
            relatorio.avatarHumanoid = avatar != null && avatar.isHuman;
            relatorio.optimizeGameObjects = importer.optimizeGameObjects;
            if (!relatorio.avatarValido || !relatorio.avatarHumanoid)
                throw new InvalidOperationException(
                    "Avatar invalido apos mapear no importador. Nao alterar o skeleton no Blender automaticamente."
                );

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var raiz = new GameObject("Human_Male_Base_v1_TESTE");
            var corpo = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            corpo.transform.SetParent(raiz.transform, false);
            var animator = corpo.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = corpo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var limites = Limites(renderers);
            relatorio.alturaMetros = limites.size.y;
            if (Mathf.Abs(limites.size.y - AlturaEsperada) > .002f)
                throw new InvalidOperationException(
                    "Altura importada inesperada: "
                        + limites.size.y
                        + " m; revisar escala do importador."
                );
            raiz.transform.position = Vector3.up * -limites.min.y;
            relatorio.pesNoChao = Limites(renderers).min.y;
            var material = CriarMaterial();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.updateWhenOffscreen = true;
                relatorio.verticesImportados += renderer.sharedMesh.vertexCount;
                using (var contagens = renderer.sharedMesh.GetBonesPerVertex())
                    foreach (var contagem in contagens)
                    {
                        relatorio.maximoInfluencias = Math.Max(
                            relatorio.maximoInfluencias,
                            contagem
                        );
                        if (contagem == 0)
                            relatorio.verticesSemPeso++;
                    }
            }
            if (relatorio.maximoInfluencias > 4 || relatorio.verticesSemPeso != 0)
                throw new InvalidOperationException("Pesos importados fora do limite.");
            foreach (var par in Mapa)
            {
                var t = animator.GetBoneTransform(par.Key);
                if (t == null || t.name != par.Value)
                    throw new InvalidOperationException("Mapeamento incorreto: " + par.Key);
                relatorio.mapeamento.Add(par.Key + " -> " + t.name);
            }
            relatorio.tPoseConferida = ConferirTPose(animator);
            relatorio.weaponRAcessivel = corpo
                .GetComponentsInChildren<Transform>(true)
                .Any(t => t.name == "Weapon.R");
            relatorio.costasAcessivel = corpo
                .GetComponentsInChildren<Transform>(true)
                .Any(t => t.name == "Costas");
            if (
                !relatorio.tPoseConferida
                || !relatorio.weaponRAcessivel
                || !relatorio.costasAcessivel
            )
                throw new InvalidOperationException(
                    "Conferir T-pose ou encaixes no importador; nao alterar o FBX."
                );

            var clipe = EncontrarAnimacao(relatorio);
            if (clipe != null)
            {
                var controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller)
                    ?? AnimatorController.CreateAnimatorControllerAtPath(Controller);
                var machine = controller.layers[0].stateMachine;
                var estado =
                    machine.states.FirstOrDefault().state ?? machine.AddState("TesteHumanoid");
                estado.motion = clipe;
                machine.defaultState = estado;
                animator.runtimeAnimatorController = controller;
                relatorio.animacao = AssetDatabase.GetAssetPath(clipe) + " | " + clipe.name;
                VerificarAnimacao(animator, relatorio, clipe);
            }
            else
            {
                relatorio.status =
                    "Parada autorizada: nenhum AnimationClip Humanoid adequado ja existe no projeto. Avatar/material/cena prontos; sem AnimatorController vazio ou animacao sintetica.";
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);
            MontarAmbiente();
            EditorSceneManager.SaveScene(raiz.scene, Cena);
            AssetDatabase.SaveAssets();
            Capturar(raiz);
            if (clipe != null)
                relatorio.status = relatorio.animacaoVerificada
                    ? "Avatar Humanoid e amostragem animada aprovados; conferir capturas."
                    : "Problema: clipe Humanoid nao movimentou o esqueleto.";
        }

        static bool ConferirTPose(Animator animator)
        {
            foreach (var lado in new[] { true, false })
            {
                var braco = animator.GetBoneTransform(
                    lado ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm
                );
                var mao = animator.GetBoneTransform(
                    lado ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand
                );
                if (
                    Mathf.Abs(braco.position.y - mao.position.y) > .06f
                    || Mathf.Abs(braco.position.x - mao.position.x) < .4f
                )
                    return false;
            }
            return animator.GetBoneTransform(HumanBodyBones.Head).position.y
                > animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
        }

        static Material CriarMaterial()
        {
            foreach (var mapa in new[] { "BaseColor", "Normal", "Roughness", "Metallic" })
            {
                var t = (TextureImporter)AssetImporter.GetAtPath(Textura(mapa));
                t.textureType =
                    mapa == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                t.sRGBTexture = mapa == "BaseColor";
                t.isReadable = mapa == "Roughness" || mapa == "Metallic";
                t.textureCompression = TextureImporterCompression.Uncompressed;
                t.maxTextureSize = mapa == "Metallic" || mapa == "Roughness" ? 4096 : 2048;
                t.SaveAndReimport();
            }
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("Metallic"));
            var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("Roughness"));
            var mp = metal.GetPixels32();
            var rp = rough.GetPixels32();
            if (metal.width != rough.width || metal.height != rough.height)
                throw new InvalidOperationException("Resolucao dos mapas de dados diverge.");
            for (var i = 0; i < mp.Length; i++)
                mp[i] = new Color32(mp[i].r, mp[i].r, mp[i].r, (byte)(255 - rp[i].r));
            var packed = new Texture2D(
                metal.width,
                metal.height,
                TextureFormat.RGBA32,
                false,
                true
            );
            packed.SetPixels32(mp);
            packed.Apply();
            var caminho = Pasta + "/HumanMale_MetallicSmoothness.png";
            File.WriteAllBytes(caminho, packed.EncodeToPNG());
            Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(caminho);
            ti.sRGBTexture = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = false;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPele);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPele);
            }
            material.SetTexture(
                "_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("BaseColor"))
            );
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture(
                "_BumpMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("Normal"))
            );
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture(
                "_MetallicGlossMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(caminho)
            );
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static string Textura(string mapa) => Pasta + "/Human_Male_Base_v1_" + mapa + ".png";

        static AnimationClip EncontrarAnimacao(Relatorio relatorio)
        {
            var caminhos = AssetDatabase
                .FindAssets("t:AnimationClip", new[] { "Assets" })
                .Concat(AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(p => p);
            var candidatos = new List<AnimationClip>();
            foreach (var caminho in caminhos)
            foreach (
                var clipe in AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<AnimationClip>()
            )
            {
                if (clipe.name.StartsWith("__preview__"))
                    continue;
                relatorio.clipesEncontrados++;
                relatorio.inventario.Add(
                    caminho
                        + " | "
                        + clipe.name
                        + " | humanMotion="
                        + clipe.humanMotion
                        + " | legacy="
                        + clipe.legacy
                );
                if (clipe.humanMotion && !clipe.legacy && clipe.length > .1f)
                    candidatos.Add(clipe);
            }
            relatorio.clipesHumanoid = candidatos.Count;
            return candidatos
                .OrderBy(c =>
                    c.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0 ? 0
                    : c.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0 ? 1
                    : 2
                )
                .ThenBy(c => c.name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        static void VerificarAnimacao(Animator animator, Relatorio relatorio, AnimationClip clipe)
        {
            animator.Rebind();
            animator.Update(0);
            var antes = Mapa.Keys.Select(b => animator.GetBoneTransform(b).position).ToArray();
            for (var quadro = 0; quadro < 60; quadro++)
                animator.Update(clipe.length / 120f);
            var depois = Mapa.Keys.Select(b => animator.GetBoneTransform(b).position).ToArray();
            relatorio.deslocamentoAnimadoMaximo = antes.Zip(depois, Vector3.Distance).Max();
            relatorio.animacaoVerificada = relatorio.deslocamentoAnimadoMaximo > .0001f;
        }

        static Bounds Limites(SkinnedMeshRenderer[] renderers)
        {
            // Renderer.bounds inclui margem conservadora do Avatar; nao mede altura nem o chao.
            var bounds = new Bounds();
            var primeiro = true;
            foreach (var renderer in renderers)
            {
                var mesh = new Mesh();
                renderer.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices)
                {
                    var world = renderer.transform.TransformPoint(vertex);
                    if (primeiro)
                    {
                        bounds = new Bounds(world, Vector3.zero);
                        primeiro = false;
                    }
                    else
                        bounds.Encapsulate(world);
                }
                Object.DestroyImmediate(mesh);
            }
            return bounds;
        }

        static void MontarAmbiente()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.65f, .68f, .73f);
            RenderSettings.ambientEquatorColor = new Color(.48f, .49f, .52f);
            RenderSettings.ambientGroundColor = new Color(.28f, .27f, .26f);
            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
            chao.name = "ChaoTeste";
            chao.transform.localScale = Vector3.one * .6f;
            Object.DestroyImmediate(chao.GetComponent<Collider>());
            var path = Pasta + "/ChaoTeste.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(.22f, .24f, .27f));
            chao.GetComponent<Renderer>().sharedMaterial = mat;
            var luz = new GameObject("LuzTeste").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.4f;
            luz.transform.rotation = Quaternion.Euler(38, -35, 0);
            var camera = new GameObject("CameraTeste").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f, .075f, .09f);
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 30f;
            camera.transform.position = new Vector3(2.8f, 1.25f, 4.5f);
            camera.transform.LookAt(new Vector3(0, .96f, 0));
            camera.fieldOfView = 30;
        }

        static void Capturar(GameObject raiz)
        {
            var camera = Camera.main;
            var posicoes = new[]
            {
                new Vector3(0, 1.1f, 4.5f),
                new Vector3(0, 1.1f, -4.5f),
                new Vector3(3.4f, 1.1f, 3.4f),
            };
            for (var i = 0; i < posicoes.Length; i++)
            {
                camera.transform.position = posicoes[i];
                camera.transform.LookAt(new Vector3(0, .96f, 0));
                var rt = RenderTexture.GetTemporary(LarguraCaptura, AlturaCaptura, 24);
                var anterior = RenderTexture.active;
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var imagem = new Texture2D(
                    LarguraCaptura,
                    AlturaCaptura,
                    TextureFormat.RGB24,
                    false
                );
                imagem.ReadPixels(new Rect(0, 0, LarguraCaptura, AlturaCaptura), 0, 0);
                imagem.Apply();
                File.WriteAllBytes(
                    Path.Combine(Saida, "human-unity-" + i + ".png"),
                    imagem.EncodeToPNG()
                );
                Object.DestroyImmediate(imagem);
                camera.targetTexture = null;
                RenderTexture.active = anterior;
                RenderTexture.ReleaseTemporary(rt);
            }
            EditorSceneManager.SaveScene(raiz.scene, Cena);
        }
    }
}
