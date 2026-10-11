using System;
using System.Collections.Generic;
using System.IO;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs.Editor
{
    /// <summary>
    /// Gera de forma reproduzivel os materiais, o AnimatorController e o prefab de cada criatura
    /// do CatalogoCriaturas a partir de modelos Quaternius (CC0). Cada prefab fica em
    /// Resources/Mobs/Criatura_&lt;id&gt; (o ApresentadorDeInimigos carrega por esse nome).
    /// Menu: Jogo do Grupo/Mobs/Gerar criaturas.
    /// </summary>
    public static class GeradorDeCriaturas
    {
        public const string CaminhoPorco = "Assets/ThirdParty/Quaternius/FarmAnimals/Pig.fbx";
        public const string CaminhoDragao = "Assets/ThirdParty/Quaternius/MonsterPack/Dragon.fbx";
        public const string PastaArte = "Assets/_Game/Features/Mobs/Art";
        public const string PastaResources = "Assets/_Game/Features/Mobs/Resources/Mobs";

        public const string ParametroMorrer = "Morrer";
        public const string ParametroAtacar = "Atacar";
        public const string EstadoParado = "Parado";
        public const string EstadoMorte = "Morte";
        public const string EstadoAtaque = "Ataque";

        // Valores ajustaveis (balanceamento visual): altura final de cada criatura, em metros.
        public const float AlturaJavali = 0.9f;
        public const float AlturaPresaVelha = 1.5f;
        public const float AlturaGuardiao = 3.5f;

        // A morte do porco nao existe no pacote: o corpo tomba de lado nesse tempo.
        const float DuracaoTombo = 0.6f;
        const float AnguloTombo = 90f;

        static readonly Color CorCorpoJavali = new Color(0.27f, 0.18f, 0.12f);
        static readonly Color CorFocinhoJavali = new Color(0.16f, 0.11f, 0.09f);
        static readonly Color CorCorpoPresaVelha = new Color(0.62f, 0.6f, 0.55f);
        static readonly Color CorFocinhoPresaVelha = new Color(0.75f, 0.68f, 0.55f);

        sealed class Receita
        {
            public int Id;
            public string Modelo;
            public float Altura;
            public string ClipeParado;
            public string ClipeAtaque; // null: sem ataque
            public string ClipeMorte; // null: tombo gerado por script
            public Color? CorCorpo;
            public Color? CorFocinho;
        }

        static readonly Receita[] Receitas =
        {
            new Receita
            {
                Id = CatalogoCriaturas.JavaliDoCampo,
                Modelo = CaminhoPorco,
                Altura = AlturaJavali,
                ClipeParado = "Armature|Idle",
                CorCorpo = CorCorpoJavali,
                CorFocinho = CorFocinhoJavali,
            },
            new Receita
            {
                Id = CatalogoCriaturas.GuardiaoDoReflexo,
                Modelo = CaminhoDragao,
                Altura = AlturaGuardiao,
                ClipeParado = "DragonArmature|Dragon_Flying",
                ClipeAtaque = "DragonArmature|Dragon_Attack",
                ClipeMorte = "DragonArmature|Dragon_Death",
            },
            new Receita
            {
                Id = CatalogoCriaturas.PresaVelha,
                Modelo = CaminhoPorco,
                Altura = AlturaPresaVelha,
                ClipeParado = "Armature|Idle",
                CorCorpo = CorCorpoPresaVelha,
                CorFocinho = CorFocinhoPresaVelha,
            },
        };

        public static string CaminhoPrefab(int id) =>
            PastaResources + "/Criatura_" + id + ".prefab";

        [MenuItem("Jogo do Grupo/Mobs/Gerar criaturas")]
        public static void Gerar()
        {
            CriarPasta(PastaArte);
            CriarPasta(PastaResources);
            foreach (var receita in Receitas)
            {
                ConfigurarImportacao(receita);
                var materiais = CriarMateriais(receita);
                var controller = CriarController(receita);
                CriarPrefab(receita, materiais, controller);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }

        static void ConfigurarImportacao(Receita receita)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(receita.Modelo);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clipes = importer.defaultClipAnimations;
            foreach (var clipe in clipes)
            {
                var repete = clipe.name == receita.ClipeParado;
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
                if (objeto is AnimationClip clipe && clipe.name == nome)
                    return clipe;
            }
            throw new InvalidOperationException("Clipe nao encontrado em " + modelo + ": " + nome);
        }

        // Materiais proprios (copiados dos do modelo, com a cor trocada) quando a receita pede.
        static Material[] CriarMateriais(Receita receita)
        {
            var originais = ObterMateriaisDoModelo(receita.Modelo);
            if (receita.CorCorpo == null)
                return originais;
            var resultado = new Material[originais.Length];
            for (var i = 0; i < originais.Length; i++)
            {
                // O corpo e o primeiro material do porco; os demais (focinho) usam a outra cor.
                var cor = i == 0 ? receita.CorCorpo.Value : receita.CorFocinho.Value;
                var caminho = PastaArte + "/Criatura_" + receita.Id + "_" + i + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
                if (material == null)
                {
                    material = new Material(originais[i]);
                    AssetDatabase.CreateAsset(material, caminho);
                }
                material.SetColor("_BaseColor", cor);
                material.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(material);
                resultado[i] = material;
            }
            return resultado;
        }

        static Material[] ObterMateriaisDoModelo(string modelo)
        {
            var prefabDoModelo = AssetDatabase.LoadAssetAtPath<GameObject>(modelo);
            var lista = new List<Material>();
            var vistos = new HashSet<Material>();
            foreach (var r in prefabDoModelo.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && vistos.Add(m))
                        lista.Add(m);
                }
            }
            return lista.ToArray();
        }

        static AnimatorController CriarController(Receita receita)
        {
            var caminho = PastaArte + "/Criatura_" + receita.Id + ".controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho) != null)
                AssetDatabase.DeleteAsset(caminho);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            controller.AddParameter(ParametroMorrer, AnimatorControllerParameterType.Trigger);
            if (receita.ClipeAtaque != null)
                controller.AddParameter(ParametroAtacar, AnimatorControllerParameterType.Trigger);

            var maquina = controller.layers[0].stateMachine;
            var parado = maquina.AddState(EstadoParado, new Vector3(250f, 0f, 0f));
            parado.motion = ObterClipe(receita.Modelo, receita.ClipeParado);
            maquina.defaultState = parado;

            AnimationClip morte;
            if (receita.ClipeMorte != null)
            {
                morte = ObterClipe(receita.Modelo, receita.ClipeMorte);
            }
            else
            {
                morte = CriarClipeDeTombo(receita.Id);
                AssetDatabase.AddObjectToAsset(morte, controller);
            }
            var estadoMorte = maquina.AddState(EstadoMorte, new Vector3(250f, 120f, 0f));
            estadoMorte.motion = morte;
            var paraMorte = maquina.AddAnyStateTransition(estadoMorte);
            paraMorte.AddCondition(AnimatorConditionMode.If, 0, ParametroMorrer);
            paraMorte.duration = 0.05f;
            paraMorte.canTransitionToSelf = false;

            if (receita.ClipeAtaque != null)
            {
                var ataque = maquina.AddState(EstadoAtaque, new Vector3(250f, -120f, 0f));
                ataque.motion = ObterClipe(receita.Modelo, receita.ClipeAtaque);
                var paraAtaque = maquina.AddAnyStateTransition(ataque);
                paraAtaque.AddCondition(AnimatorConditionMode.If, 0, ParametroAtacar);
                paraAtaque.duration = 0.05f;
                paraAtaque.canTransitionToSelf = false;
                var voltar = ataque.AddTransition(parado);
                voltar.hasExitTime = true;
                voltar.exitTime = 0.9f;
                voltar.duration = 0.1f;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Clipe de morte do porco: tomba de lado (o pacote so traz idle e pulo).
        static AnimationClip CriarClipeDeTombo(int id)
        {
            var clipe = new AnimationClip { name = "Criatura_" + id + "_Tombo" };
            var curva = AnimationCurve.EaseInOut(0f, 0f, DuracaoTombo, AnguloTombo);
            clipe.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", curva);
            return clipe;
        }

        static void CriarPrefab(
            Receita receita,
            Material[] materiais,
            AnimatorController controller
        )
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(receita.Modelo);
            var raiz = new GameObject("Criatura_" + receita.Id);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                instancia.name = "Modelo";
                instancia.transform.SetParent(raiz.transform, false);

                if (receita.CorCorpo != null)
                {
                    var originais = ObterMateriaisDoModelo(receita.Modelo);
                    foreach (var r in instancia.GetComponentsInChildren<Renderer>())
                        r.sharedMaterials = Trocar(r.sharedMaterials, originais, materiais);
                }

                // Escala para a altura alvo e pes em Y=0. A raiz fica em escala 1: o servidor manda
                // a escala da instancia (ApresentadorDeInimigos).
                var limites = Limites(instancia);
                var escala = receita.Altura / limites.size.y;
                instancia.transform.localScale = Vector3.one * escala;
                limites = Limites(instancia);
                instancia.transform.localPosition = new Vector3(0f, -limites.min.y, 0f);
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "gerador-mobs.txt"),
                    receita.Id + " escala=" + escala + " altura=" + limites.size + "\n"
                );

                var animator = instancia.GetComponent<Animator>();
                if (animator == null)
                    animator = instancia.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab(receita.Id));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        static Material[] Trocar(Material[] atuais, Material[] originais, Material[] novos)
        {
            var resultado = new Material[atuais.Length];
            for (var i = 0; i < atuais.Length; i++)
            {
                var indice = Array.IndexOf(originais, atuais[i]);
                resultado[i] = indice >= 0 ? novos[indice] : atuais[i];
            }
            return resultado;
        }

        static Bounds Limites(GameObject objeto)
        {
            var rs = objeto.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs)
                b.Encapsulate(r.bounds);
            return b;
        }
    }
}
