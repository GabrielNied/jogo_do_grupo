using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Editor
{
    /// <summary>
    /// Gera de forma reproduzivel o material, o AnimatorController e o prefab do jogador de cada
    /// raca a partir dos modelos proprios feitos no Blender por script (arte/blender/personagens/,
    /// padrao em docs/arte/padrao-de-modelos.md). Cada prefab fica em
    /// Resources/Personagem/Jogador_&lt;Raca&gt;.
    /// Menu: Jogo do Grupo/Personagem/Gerar jogadores das racas.
    /// </summary>
    public static class GeradorJogadoresDasRacas
    {
        public const string PastaArte = "Assets/_Game/Features/Personagem/Art";
        public const string PastaModelos = PastaArte + "/Modelos";
        public const string PastaResources =
            "Assets/_Game/Features/Personagem/Resources/Personagem";

        public const string ParametroVelocidade = "Velocidade";
        public const string ParametroNoChao = "NoChao";
        public const string ParametroEsquiva = "Esquiva";

        /// <summary>Nomes dos encaixes (filhos dos ossos) para o artista de itens.</summary>
        public const string EncaixeMaoDireita = "EncaixeMaoDireita";
        public const string EncaixeCostas = "EncaixeCostas";
        public const string OssoMaoDireita = "Weapon.R";
        public const string OssoCostas = "Costas";

        // Nomes dos clipes (iguais aos das acoes no Blender).
        public const string ClipeParado = "Idle";
        public const string ClipeAndar = "Andar";
        public const string ClipeCorrer = "Correr";
        public const string ClipeEsquivar = "Esquivar";
        public const string ClipeAtacar = "Atacar";
        public const string ClipeMorrer = "Morrer";
        public const string ClipeDano = "Dano";

        public static readonly string[] Clipes =
        {
            ClipeParado,
            ClipeAndar,
            ClipeCorrer,
            ClipeEsquivar,
            ClipeAtacar,
            ClipeMorrer,
            ClipeDano,
        };

        /// <summary>Raca do jogo, no mesmo nome do enum Raca (sem acento).</summary>
        public static readonly string[] Racas =
        {
            "Valdren",
            "Aurideo",
            "Granitico",
            "Cinereo",
            "Silvano",
        };

        // Valores ajustaveis (balanceamento visual).
        public const float VelocidadeAndar = 2f;
        public const float VelocidadeCorrer = 5f;
        const float LisuraPele = 0.12f;
        const float IntensidadeEmissao = 2.0f;
        const int TamanhoMaximoTextura = 1024;
        const float TransicaoEsquiva = 0.05f;
        const float SaidaDaEsquiva = 0.85f;
        const float VoltaDaEsquiva = 0.12f;

        public static string CaminhoModelo(string raca) => PastaModelos + "/" + raca + ".fbx";

        public static string CaminhoTextura(string raca, string mapa) =>
            PastaModelos + "/" + raca + "_" + mapa + ".png";

        public static string CaminhoMaterial(string raca) =>
            PastaArte + "/Jogador_" + raca + ".mat";

        public static string CaminhoController(string raca) =>
            PastaArte + "/Jogador_" + raca + ".controller";

        public static string CaminhoPrefab(string raca) =>
            PastaResources + "/Jogador_" + raca + ".prefab";

        [MenuItem("Jogo do Grupo/Personagem/Gerar jogadores das racas")]
        public static void Gerar()
        {
            CriarPasta(PastaArte);
            CriarPasta(PastaResources);
            foreach (var raca in Racas)
            {
                var emissao = File.Exists(CaminhoTextura(raca, "Emissao"));
                ConfigurarTextura(CaminhoTextura(raca, "Cor"), true);
                if (emissao)
                    ConfigurarTextura(CaminhoTextura(raca, "Emissao"), true);
                ConfigurarModelo(raca);
                var material = CriarMaterial(raca, emissao);
                var controller = CriarController(raca);
                CriarPrefab(raca, material, controller);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GeradorJogadoresDasRacas] Gerados " + Racas.Length + " prefabs.");
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }

        static void ConfigurarTextura(string caminho, bool cor)
        {
            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Textura nao encontrada: " + caminho);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = cor;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = TamanhoMaximoTextura;
            importer.anisoLevel = 4;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        static void ConfigurarModelo(string raca)
        {
            var caminho = CaminhoModelo(raca);
            var importer = AssetImporter.GetAtPath(caminho) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Modelo nao encontrado: " + caminho);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;
            // Generic: o Animator fica no proprio modelo e os clipes vem do mesmo FBX.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clipes = importer.defaultClipAnimations;
            foreach (var clipe in clipes)
            {
                // O Blender exporta "<Armadura>|<Acao>"; o clipe fica so com o nome da acao.
                var nome = clipe.takeName;
                var barra = nome.LastIndexOf('|');
                clipe.name = barra >= 0 ? nome.Substring(barra + 1) : nome;
                var repete =
                    clipe.name == ClipeParado
                    || clipe.name == ClipeAndar
                    || clipe.name == ClipeCorrer;
                clipe.loopTime = repete;
                clipe.loopPose = repete;
            }
            importer.clipAnimations = clipes;
            importer.SaveAndReimport();
        }

        static AnimationClip ObterClipe(string modelo, string nome)
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(modelo))
            {
                if (
                    objeto is AnimationClip clipe
                    && clipe.name == nome
                    && !clipe.name.StartsWith("__preview__")
                )
                    return clipe;
            }
            throw new InvalidOperationException("Clipe nao encontrado em " + modelo + ": " + nome);
        }

        static Material CriarMaterial(string raca, bool emissao)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Shader URP Lit nao encontrado.");
            var caminho = CaminhoMaterial(raca);
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }
            material.shader = shader;
            material.name = "Jogador_" + raca;

            var cor = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura(raca, "Cor"));
            material.SetTexture("_BaseMap", cor);
            material.mainTexture = cor;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", LisuraPele);

            if (emissao)
            {
                material.SetTexture(
                    "_EmissionMap",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura(raca, "Emissao"))
                );
                material.SetColor("_EmissionColor", Color.white * IntensidadeEmissao);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetTexture("_EmissionMap", null);
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimatorController CriarController(string raca)
        {
            var caminho = CaminhoController(raca);
            var modelo = CaminhoModelo(raca);
            // Recria do zero para o resultado ser sempre o mesmo.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho) != null)
                AssetDatabase.DeleteAsset(caminho);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            controller.AddParameter(ParametroVelocidade, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParametroNoChao, AnimatorControllerParameterType.Bool);
            controller.AddParameter(ParametroEsquiva, AnimatorControllerParameterType.Trigger);

            var parametros = controller.parameters;
            foreach (var p in parametros)
                if (p.name == ParametroNoChao)
                    p.defaultBool = true;
            controller.parameters = parametros;

            var maquina = controller.layers[0].stateMachine;
            controller.CreateBlendTreeInController("Movimento", out var arvore);
            arvore.blendParameter = ParametroVelocidade;
            arvore.useAutomaticThresholds = false;
            arvore.AddChild(ObterClipe(modelo, ClipeParado), 0f);
            arvore.AddChild(ObterClipe(modelo, ClipeAndar), VelocidadeAndar);
            arvore.AddChild(ObterClipe(modelo, ClipeCorrer), VelocidadeCorrer);
            var movimento = BuscarEstado(maquina, "Movimento");
            maquina.defaultState = movimento;

            var esquiva = maquina.AddState("Esquiva", new Vector3(300f, 0f, 0f));
            esquiva.motion = ObterClipe(modelo, ClipeEsquivar);

            var qualquerParaEsquiva = maquina.AddAnyStateTransition(esquiva);
            qualquerParaEsquiva.AddCondition(AnimatorConditionMode.If, 0, ParametroEsquiva);
            qualquerParaEsquiva.duration = TransicaoEsquiva;
            qualquerParaEsquiva.canTransitionToSelf = false;

            var esquivaParaMovimento = esquiva.AddTransition(movimento);
            esquivaParaMovimento.hasExitTime = true;
            esquivaParaMovimento.exitTime = SaidaDaEsquiva;
            esquivaParaMovimento.duration = VoltaDaEsquiva;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimatorState BuscarEstado(AnimatorStateMachine maquina, string nome)
        {
            foreach (var filho in maquina.states)
                if (filho.state.name == nome)
                    return filho.state;
            return null;
        }

        static void CriarPrefab(string raca, Material material, AnimatorController controller)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo(raca));
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                instancia.name = "Jogador_" + raca;
                // O FBX ja sai com 1,8 m, pes em Y=0 e frente em +Z.
                instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instancia.transform.localScale = Vector3.one;

                foreach (var r in instancia.GetComponentsInChildren<Renderer>())
                    r.sharedMaterials = Preencher(material, r.sharedMaterials.Length);

                CriarEncaixe(instancia.transform, OssoMaoDireita, EncaixeMaoDireita);
                CriarEncaixe(instancia.transform, OssoCostas, EncaixeCostas);

                var animator = instancia.GetComponent<Animator>();
                if (animator == null)
                    animator = instancia.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(instancia, CaminhoPrefab(raca));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instancia);
            }
        }

        static void CriarEncaixe(Transform raiz, string osso, string nome)
        {
            var pai = BuscarOsso(raiz, osso);
            var encaixe = new GameObject(nome).transform;
            encaixe.SetParent(pai, false);
            encaixe.localPosition = Vector3.zero;
            encaixe.localRotation = Quaternion.identity;
            encaixe.localScale = Vector3.one;
        }

        static Material[] Preencher(Material material, int quantidade)
        {
            var lista = new List<Material>();
            for (var i = 0; i < quantidade; i++)
                lista.Add(material);
            return lista.ToArray();
        }

        static Transform BuscarOsso(Transform raiz, string nome)
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>())
                if (t.name == nome)
                    return t;
            throw new InvalidOperationException("Osso nao encontrado: " + nome);
        }
    }
}
