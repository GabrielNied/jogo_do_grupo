using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Editor
{
    /// <summary>
    /// Gera de forma reproduzivel o material, o AnimatorController e o prefab do jogador humano
    /// a partir do modelo Quaternius (CC0). Menu: Jogo do Grupo/Personagem/Gerar jogador humano.
    /// </summary>
    public static class GeradorJogadorHumano
    {
        public const string CaminhoModelo = "Assets/ThirdParty/Quaternius/RPGCharacters/Monk.fbx";
        public const string CaminhoTextura =
            "Assets/ThirdParty/Quaternius/RPGCharacters/Monk_Texture.png";
        public const string PastaArte = "Assets/_Game/Features/Personagem/Art";
        public const string CaminhoMaterial = PastaArte + "/Jogador_Humano.mat";
        public const string CaminhoController = PastaArte + "/Jogador_Humano.controller";
        public const string PastaResources =
            "Assets/_Game/Features/Personagem/Resources/Personagem";
        public const string CaminhoPrefab = PastaResources + "/Jogador_Humano.prefab";

        public const string ParametroVelocidade = "Velocidade";
        public const string ParametroNoChao = "NoChao";
        public const string ParametroEsquiva = "Esquiva";

        /// <summary>Altura do personagem em metros.</summary>
        public const float AlturaAlvo = 1.8f;

        /// <summary>Velocidades (m/s) em que cada clipe do blend tree toca sozinho.</summary>
        public const float VelocidadeAndar = 2f;
        public const float VelocidadeCorrer = 5f;

        // Nomes dos clipes dentro do FBX.
        const string ClipeParado = "CharacterArmature|Idle";
        const string ClipeAndar = "CharacterArmature|Walk";
        const string ClipeCorrer = "CharacterArmature|Run";
        const string ClipeRoll = "CharacterArmature|Roll";
        const string OssoTopoDaCabeca = "Head_end";

        [MenuItem("Jogo do Grupo/Personagem/Gerar jogador humano")]
        public static void Gerar()
        {
            ConfigurarImportacao();
            var material = CriarMaterial();
            var controller = CriarController();
            CriarPrefab(material, controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GeradorJogadorHumano] Gerado: " + CaminhoPrefab);
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }

        static void ConfigurarImportacao()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(CaminhoModelo);
            // Generic: o Animator esta no proprio modelo e os clipes vem do mesmo FBX.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            var clipes = importer.defaultClipAnimations;
            foreach (var clipe in clipes)
            {
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

        static Material CriarMaterial()
        {
            CriarPasta(PastaArte);
            var material = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, CaminhoMaterial);
            }
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura);
            material.SetTexture("_BaseMap", textura);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimationClip ObterClipe(string nome)
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo))
            {
                if (objeto is AnimationClip clipe && clipe.name == nome)
                    return clipe;
            }
            var achados = new List<string>();
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo))
                achados.Add(objeto.GetType().Name + ":" + objeto.name);
            throw new System.InvalidOperationException(
                "Clipe nao encontrado no modelo: "
                    + nome
                    + ". Achados: "
                    + string.Join(", ", achados)
            );
        }

        static AnimatorController CriarController()
        {
            CriarPasta(PastaArte);
            // Recria do zero para o resultado ser sempre o mesmo.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CaminhoController) != null)
                AssetDatabase.DeleteAsset(CaminhoController);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);
            controller.AddParameter(ParametroVelocidade, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParametroNoChao, AnimatorControllerParameterType.Bool);
            controller.AddParameter(ParametroEsquiva, AnimatorControllerParameterType.Trigger);

            var parametros = controller.parameters;
            foreach (var p in parametros)
                if (p.name == ParametroNoChao)
                    p.defaultBool = true;
            controller.parameters = parametros;

            var maquina = controller.layers[0].stateMachine;
            var estado = controller.CreateBlendTreeInController("Movimento", out var arvore);
            arvore.blendParameter = ParametroVelocidade;
            arvore.useAutomaticThresholds = false;
            arvore.AddChild(ObterClipe(ClipeParado), 0f);
            arvore.AddChild(ObterClipe(ClipeAndar), VelocidadeAndar);
            arvore.AddChild(ObterClipe(ClipeCorrer), VelocidadeCorrer);
            var movimento = BuscarEstado(maquina, "Movimento");
            maquina.defaultState = movimento;

            var esquiva = maquina.AddState("Esquiva", new Vector3(300f, 0f, 0f));
            esquiva.motion = ObterClipe(ClipeRoll);

            var qualquerParaEsquiva = maquina.AddAnyStateTransition(esquiva);
            qualquerParaEsquiva.AddCondition(AnimatorConditionMode.If, 0, ParametroEsquiva);
            qualquerParaEsquiva.duration = 0.05f;
            qualquerParaEsquiva.canTransitionToSelf = false;

            var esquivaParaMovimento = esquiva.AddTransition(movimento);
            esquivaParaMovimento.hasExitTime = true;
            esquivaParaMovimento.exitTime = 0.85f;
            esquivaParaMovimento.duration = 0.12f;

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

        static void CriarPrefab(Material material, AnimatorController controller)
        {
            CriarPasta(PastaResources);
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                instancia.name = "Jogador_Humano";
                instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instancia.transform.localScale = Vector3.one;

                foreach (var r in instancia.GetComponentsInChildren<Renderer>())
                    r.sharedMaterials = Preencher(material, r.sharedMaterials.Length);

                // Os limites do SkinnedMeshRenderer nao sao confiaveis; a altura vem do osso do topo da
                // cabeca. A origem do modelo ja esta no chao (pes em Y=0) e a frente e +Z.
                var topo = BuscarOsso(instancia.transform, OssoTopoDaCabeca);
                var escala = AlturaAlvo / topo.position.y;
                instancia.transform.localScale = Vector3.one * escala;
                Debug.Log("[GeradorJogadorHumano] altura=" + topo.position.y + " escala=" + escala);

                var animator = instancia.GetComponent<Animator>();
                if (animator == null)
                    animator = instancia.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(instancia, CaminhoPrefab);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
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
            throw new System.InvalidOperationException("Osso nao encontrado: " + nome);
        }
    }
}
