using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor
{
    /// <summary>Reproduz a animacao fornecida via Mecanim; dados Mixamo ficam somente locais.</summary>
    public static class ValidacaoRetargetHumanMale
    {
        const string Pasta = "Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest";
        const string Local = Pasta + "/ValidacaoLocal";
        const string Movimento = Local + "/OldManIdle.fbx";
        const string Corpo = Pasta + "/Human_Male_Base_v1_UnityTest.fbx";
        const float Passo = 1f / 60f;

        [Serializable]
        sealed class Quadro
        {
            public float tempo;
            public float normalizedTime;
            public float peEsquerdoY;
            public float peDireitoY;
            public float joelhoEsquerdoGraus;
            public float joelhoDireitoGraus;
            public float altura;
            public Vector3 raiz;
            public Vector3 quadril;
            public Vector3 deltaRoot;
            public List<string> capturas = new List<string>();
        }

        [Serializable]
        sealed class Relatorio
        {
            public string hashCorpoAntes;
            public string hashCorpoDepois;
            public string hashIdleAntes;
            public string hashIdleDepois;
            public string animacao;
            public string cena = Local + "/HumanMaleIdle.unity";
            public bool avatarFonteValido;
            public bool avatarFonteHuman;
            public bool avatarDestinoValido;
            public bool avatarDestinoHuman;
            public float escalaHumanaFonte;
            public float escalaHumanaDestino;
            public bool clipHumanMotion;
            public float duracaoClip;
            public bool rootMotionAplicado;
            public bool footIK;
            public float ajusteChao;
            public float tempoNormalizadoFinal;
            public float deslocamentoQuadril;
            public float deslocamentoRaiz;
            public float maiorDeltaRoot;
            public float menorPeY;
            public float maiorPeApoiadoY;
            public bool transformsFinitos;
            public bool arquivosPreservados;
            public bool reproducaoConfirmada;
            public int posesVisuaisDistintas;
            public string resultado;
            public List<Quadro> quadros = new List<Quadro>();
            public List<string> mapaFonte = new List<string>();
            public List<string> mensagens = new List<string>();
        }

        static string Saida =>
            Environment.GetEnvironmentVariable("HUMAN_UNITY_TEST_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "human-male-unity03b");

        [MenuItem("Jogo do Grupo/Personagem/Validar retarget Human Male com Idle local")]
        public static void Executar()
        {
            Directory.CreateDirectory(Saida);
            var r = new Relatorio { transformsFinitos = true, menorPeY = float.MaxValue };
            Application.LogCallback registrar = (msg, stack, tipo) =>
            {
                if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Warning)
                    r.mensagens.Add(tipo + ": " + msg);
            };
            Application.logMessageReceived += registrar;
            try
            {
                Validar(r);
            }
            catch (Exception erro)
            {
                r.resultado = "ERRO: " + erro.Message;
                throw;
            }
            finally
            {
                Application.logMessageReceived -= registrar;
                File.WriteAllText(
                    Path.Combine(Saida, "validacao-unity03b.json"),
                    JsonUtility.ToJson(r, true)
                );
                Debug.Log("[RetargetHumanMale] " + r.resultado);
            }
        }

        static string Hash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter
                    .ToString(sha.ComputeHash(File.ReadAllBytes(path)))
                    .Replace("-", "")
                    .ToLowerInvariant();
        }

        static void Validar(Relatorio r)
        {
            var fonte =
                Environment.GetEnvironmentVariable("HUMAN_IDLE_FBX")
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "Old Man Idle.fbx"
                );
            if (!File.Exists(fonte))
                throw new FileNotFoundException("Forneca HUMAN_IDLE_FBX.", fonte);
            r.hashIdleAntes = Hash(fonte);
            r.hashCorpoAntes = Hash(Corpo);
            if (!AssetDatabase.IsValidFolder(Local))
                AssetDatabase.CreateFolder(Pasta, "ValidacaoLocal");
            if (!File.Exists(Movimento) || Hash(Movimento) != r.hashIdleAntes)
                File.Copy(fonte, Movimento, true);
            AssetDatabase.ImportAsset(Movimento, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Movimento);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.SaveAndReimport();
            var modeloFonte = AssetDatabase.LoadAssetAtPath<GameObject>(Movimento);
            var avatarFonte = modeloFonte.GetComponent<Animator>()?.avatar;
            if (avatarFonte == null || !avatarFonte.isValid || !avatarFonte.isHuman)
            {
                var hd = importer.humanDescription;
                var nomes = modeloFonte.GetComponentsInChildren<Transform>(true);
                var mapa = new Dictionary<string, string>
                {
                    { "Hips", "Hips" },
                    { "Spine", "Spine" },
                    { "Chest", "Spine1" },
                    { "UpperChest", "Spine2" },
                    { "Neck", "Neck" },
                    { "Head", "Head" },
                };
                foreach (var lado in new[] { "Left", "Right" })
                foreach (
                    var par in new[]
                    {
                        ("Shoulder", "Shoulder"),
                        ("UpperArm", "Arm"),
                        ("LowerArm", "ForeArm"),
                        ("Hand", "Hand"),
                        ("UpperLeg", "UpLeg"),
                        ("LowerLeg", "Leg"),
                        ("Foot", "Foot"),
                        ("Toes", "ToeBase"),
                    }
                )
                    mapa[lado + par.Item1] = lado + par.Item2;
                hd.human = mapa.Select(par => new HumanBone
                    {
                        humanName = par.Key,
                        boneName = nomes.Single(t => t.name.Split(':').Last() == par.Value).name,
                        limit = new HumanLimit { useDefaultValues = true },
                    })
                    .ToArray();
                hd.skeleton = nomes
                    .Select(t => new SkeletonBone
                    {
                        name = t.name,
                        position = t.localPosition,
                        rotation = t.localRotation,
                        scale = t.localScale,
                    })
                    .ToArray();
                importer.humanDescription = hd;
                importer.SaveAndReimport();
                modeloFonte = AssetDatabase.LoadAssetAtPath<GameObject>(Movimento);
                avatarFonte = modeloFonte.GetComponent<Animator>()?.avatar;
            }
            r.avatarFonteValido = avatarFonte != null && avatarFonte.isValid;
            r.avatarFonteHuman = avatarFonte != null && avatarFonte.isHuman;
            if (!r.avatarFonteValido || !r.avatarFonteHuman)
                throw new InvalidOperationException(
                    "Avatar da Idle invalido; nao alterar o skeleton do personagem."
                );
            r.mapaFonte = avatarFonte
                .humanDescription.human.Select(h => h.humanName + " -> " + h.boneName)
                .ToList();
            var clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                throw new InvalidOperationException("FBX fornecido sem takes.");
            foreach (var c in clips)
            {
                c.loopTime = true;
                c.loopPose = true;
                c.lockRootRotation = true;
                c.keepOriginalOrientation = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalPositionXZ = true;
                c.lockRootHeightY = true;
                c.keepOriginalPositionY = false;
                c.heightFromFeet = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            var clip = AssetDatabase
                .LoadAllAssetsAtPath(Movimento)
                .OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__") && c.humanMotion && c.length > 0);
            r.animacao = clip.name;
            r.clipHumanMotion = clip.humanMotion;
            r.duracaoClip = clip.length;

            var cenaBase = Pasta + "/HumanMaleHumanoid.unity";
            EditorSceneManager.OpenScene(cenaBase);
            var raiz = GameObject.Find("Human_Male_Base_v1_TESTE");
            var animator = raiz.GetComponentInChildren<Animator>();
            r.avatarDestinoValido = animator.avatar.isValid;
            r.avatarDestinoHuman = animator.avatar.isHuman;
            r.escalaHumanaDestino = animator.humanScale;
            var instFonte = Object.Instantiate(modeloFonte);
            r.escalaHumanaFonte = instFonte.GetComponent<Animator>().humanScale;
            Object.DestroyImmediate(instFonte);
            if (!r.avatarDestinoValido || !r.avatarDestinoHuman)
                throw new InvalidOperationException("Avatar destino deixou de ser valido.");

            var pathController = Local + "/IdleRetarget.controller";
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(pathController)
                ?? AnimatorController.CreateAnimatorControllerAtPath(pathController);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.FirstOrDefault().state ?? machine.AddState("IdleRetarget");
            state.motion = clip;
            state.iKOnFeet = true;
            machine.defaultState = state;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            r.rootMotionAplicado = animator.applyRootMotion;
            r.footIK = state.iKOnFeet;
            var skins = raiz.GetComponentsInChildren<SkinnedMeshRenderer>();
            var gruposPes = IdentificarPes(skins, animator);
            animator.Rebind();
            animator.Play("IdleRetarget", 0, 0);
            animator.Update(0);
            var peInicial = Medir(skins, gruposPes);
            r.ajusteChao = -Mathf.Min(peInicial.Item1, peInicial.Item2);
            raiz.transform.position += Vector3.up * r.ajusteChao;
            var raizInicial = animator.transform.position;
            var quadrilInicial = animator.GetBoneTransform(HumanBodyBones.Hips).position;
            var camera = Camera.main;
            var anteriorTransform = camera.transform.position;
            var anteriorRotacao = camera.transform.rotation;
            int passos = Mathf.CeilToInt(clip.length * 2 * 60);
            int intervaloCaptura = Math.Max(1, Mathf.RoundToInt(clip.length * 60 / 8));
            for (var i = 0; i <= passos; i++)
            {
                if (i > 0)
                    animator.Update(Passo);
                r.maiorDeltaRoot = Mathf.Max(r.maiorDeltaRoot, animator.deltaPosition.magnitude);
                r.deslocamentoRaiz = Mathf.Max(
                    r.deslocamentoRaiz,
                    Vector3.Distance(raizInicial, animator.transform.position)
                );
                r.deslocamentoQuadril = Mathf.Max(
                    r.deslocamentoQuadril,
                    Vector3.Distance(
                        quadrilInicial,
                        animator.GetBoneTransform(HumanBodyBones.Hips).position
                    )
                );
                foreach (var t in raiz.GetComponentsInChildren<Transform>())
                    r.transformsFinitos &= Finito(t.position) && Finito(t.lossyScale);
                if (i % 6 != 0 && i % intervaloCaptura != 0 && i != passos)
                    continue;
                var pes = Medir(skins, gruposPes);
                var q = new Quadro
                {
                    tempo = i * Passo,
                    normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    peEsquerdoY = pes.Item1,
                    peDireitoY = pes.Item2,
                    altura = pes.Item3,
                    raiz = animator.transform.position,
                    quadril = animator.GetBoneTransform(HumanBodyBones.Hips).position,
                    deltaRoot = animator.deltaPosition,
                    joelhoEsquerdoGraus = AnguloJoelho(animator, true),
                    joelhoDireitoGraus = AnguloJoelho(animator, false),
                };
                r.menorPeY = Mathf.Min(r.menorPeY, pes.Item1, pes.Item2);
                r.maiorPeApoiadoY = Mathf.Max(r.maiorPeApoiadoY, Mathf.Min(pes.Item1, pes.Item2));
                if (i % intervaloCaptura == 0 && i < clip.length * 60)
                {
                    var fase = i / intervaloCaptura;
                    Capturar(camera, new Vector3(0, 1.1f, 4.5f), "idle-" + fase + "-frente.png", q);
                    Capturar(camera, new Vector3(4.5f, 1.1f, 0), "idle-" + fase + "-lado.png", q);
                }
                r.quadros.Add(q);
            }
            r.tempoNormalizadoFinal = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            r.posesVisuaisDistintas = Directory
                .GetFiles(Saida, "idle-*-frente.png")
                .Select(Hash)
                .Distinct()
                .Count();
            r.reproducaoConfirmada =
                r.tempoNormalizadoFinal > 1.8f
                && r.deslocamentoQuadril > .0001f
                && r.transformsFinitos
                && r.posesVisuaisDistintas > 1;
            camera.transform.SetPositionAndRotation(anteriorTransform, anteriorRotacao);
            animator.Rebind();
            animator.Play("IdleRetarget", 0, 0);
            animator.Update(0);
            PrefabUtility.SaveAsPrefabAsset(raiz, Local + "/HumanMaleIdle.prefab");
            EditorSceneManager.SaveScene(raiz.scene, r.cena);
            AssetDatabase.SaveAssets();
            r.hashCorpoDepois = Hash(Corpo);
            r.hashIdleDepois = Hash(fonte);
            r.arquivosPreservados =
                r.hashCorpoAntes == r.hashCorpoDepois && r.hashIdleAntes == r.hashIdleDepois;
            if (!r.arquivosPreservados)
                throw new InvalidOperationException("Hash de origem alterado.");
            r.resultado = r.reproducaoConfirmada
                ? "REPRODUCAO CONFIRMADA por dois ciclos Mecanim a 60 Hz; aguarda inspecao visual das capturas e contato dos pes."
                : "PROBLEMA: reproducao/retarget nao confirmado; nao aprovar automaticamente.";
        }

        static bool Finito(Vector3 v) =>
            !float.IsNaN(v.x)
            && !float.IsNaN(v.y)
            && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x)
            && !float.IsInfinity(v.y)
            && !float.IsInfinity(v.z);

        static float AnguloJoelho(Animator a, bool esquerdo)
        {
            var coxa = a.GetBoneTransform(
                esquerdo ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg
            ).position;
            var joelho = a.GetBoneTransform(
                esquerdo ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg
            ).position;
            var pe = a.GetBoneTransform(
                esquerdo ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot
            ).position;
            return Vector3.Angle(coxa - joelho, pe - joelho);
        }

        static List<int[]> IdentificarPes(SkinnedMeshRenderer[] skins, Animator a)
        {
            var e = a.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            var d = a.GetBoneTransform(HumanBodyBones.RightFoot).position;
            var grupos = new List<int[]>();
            foreach (var s in skins)
            {
                var mesh = new Mesh();
                s.BakeMesh(mesh);
                grupos.Add(
                    mesh.vertices.Select(v =>
                        {
                            var p = s.transform.TransformPoint(v);
                            return p.y < .16f
                                ? (Vector3.Distance(p, e) < Vector3.Distance(p, d) ? 1 : 2)
                                : 0;
                        })
                        .ToArray()
                );
                Object.DestroyImmediate(mesh);
            }
            return grupos;
        }

        static (float, float, float) Medir(SkinnedMeshRenderer[] skins, List<int[]> grupos)
        {
            float e = float.MaxValue,
                d = float.MaxValue,
                min = float.MaxValue,
                max = float.MinValue;
            for (var j = 0; j < skins.Length; j++)
            {
                var s = skins[j];
                var mesh = new Mesh();
                s.BakeMesh(mesh);
                var vs = mesh.vertices;
                for (var i = 0; i < vs.Length; i++)
                {
                    var p = s.transform.TransformPoint(vs[i]);
                    min = Mathf.Min(min, p.y);
                    max = Mathf.Max(max, p.y);
                    if (grupos[j][i] == 1)
                        e = Mathf.Min(e, p.y);
                    if (grupos[j][i] == 2)
                        d = Mathf.Min(d, p.y);
                }
                Object.DestroyImmediate(mesh);
            }
            if (e == float.MaxValue || d == float.MaxValue)
                throw new InvalidOperationException("Grupos dos pes vazios.");
            return (e, d, max - min);
        }

        static void Capturar(Camera camera, Vector3 posicao, string nome, Quadro quadro)
        {
            // Camera.Render pode reutilizar skinning GPU no mesmo frame de editor.
            // BakeMesh fotografa a pose CPU avaliada pelo Animator, sem mudar o asset.
            var skins = Object
                .FindObjectsByType<SkinnedMeshRenderer>()
                .Where(s => s.enabled)
                .ToArray();
            var snapshots = new List<GameObject>();
            var meshes = new List<Mesh>();
            foreach (var skin in skins)
            {
                var mesh = new Mesh();
                skin.BakeMesh(mesh);
                var snapshot = new GameObject("CapturaTemporariaDaPose");
                snapshot.transform.SetParent(skin.transform, false);
                snapshot.AddComponent<MeshFilter>().sharedMesh = mesh;
                snapshot.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                skin.enabled = false;
                snapshots.Add(snapshot);
                meshes.Add(mesh);
            }
            camera.transform.position = posicao;
            camera.transform.LookAt(new Vector3(0, .95f, 0));
            var rt = RenderTexture.GetTemporary(1024, 1152, 24);
            var antes = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(1024, 1152, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1024, 1152), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Saida, nome), image.EncodeToPNG());
                Object.DestroyImmediate(image);
                quadro.capturas.Add(nome);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = antes;
                RenderTexture.ReleaseTemporary(rt);
                foreach (var snapshot in snapshots)
                    Object.DestroyImmediate(snapshot);
                foreach (var mesh in meshes)
                    Object.DestroyImmediate(mesh);
                foreach (var skin in skins)
                    skin.enabled = true;
            }
        }
    }
}
