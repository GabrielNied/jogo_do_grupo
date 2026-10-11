using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs.Editor
{
    /// <summary>
    /// Gera de forma reproduzivel o material, o AnimatorController e o prefab de cada criatura
    /// do CatalogoCriaturas a partir dos modelos proprios feitos no Blender por script
    /// (arte/blender/, padrao em docs/arte/padrao-de-modelos.md). Cada prefab fica em
    /// Resources/Mobs/Criatura_&lt;id&gt; (o ApresentadorDeInimigos carrega por esse nome).
    /// Menu: Jogo do Grupo/Mobs/Gerar criaturas.
    /// </summary>
    public static class GeradorDeCriaturas
    {
        public const string PastaArte = "Assets/_Game/Features/Mobs/Art";
        public const string PastaModelos = PastaArte + "/Modelos";
        public const string PastaResources = "Assets/_Game/Features/Mobs/Resources/Mobs";

        public const string ParametroMorrer = "Morrer";
        public const string ParametroAtacar = "Atacar";
        public const string ParametroAndando = "Andando";

        // Nomes dos clipes (iguais aos das acoes no Blender) e dos estados do controlador.
        public const string ClipeParado = "Idle";
        public const string ClipeAndar = "Andar";
        public const string ClipeAtaque = "Atacar";
        public const string ClipeMorte = "Morrer";
        public static readonly string[] Clipes =
        {
            ClipeParado,
            ClipeAndar,
            ClipeAtaque,
            ClipeMorte,
        };

        // Mapas de textura que o Blender grava ao lado do FBX: <Nome>_<Mapa>.png.
        public const string MapaCor = "Cor";
        public const string MapaBrilho = "Brilho"; // R = metal, A = lisura
        public const string MapaEmissao = "Emissao";

        // Valores ajustaveis (balanceamento visual).
        const float LisuraPelo = 0.12f;
        const float IntensidadeEmissao = 2.2f;
        const int TamanhoMaximoTextura = 1024;
        const float TransicaoAndar = 0.15f;
        const float TransicaoAtaque = 0.08f;
        const float TransicaoMorte = 0.05f;
        const float VoltaDoAtaque = 0.15f;

        sealed class Receita
        {
            public int Id;
            public string Nome;
            public bool TemBrilho;
            public bool TemEmissao;
        }

        static readonly Receita[] Receitas =
        {
            new Receita { Id = CatalogoCriaturas.JavaliDoCampo, Nome = "JavaliDoCampo" },
            new Receita
            {
                Id = CatalogoCriaturas.GuardiaoDoReflexo,
                Nome = "GuardiaoDoReflexo",
                TemBrilho = true,
                TemEmissao = true,
            },
            new Receita { Id = CatalogoCriaturas.PresaVelha, Nome = "PresaVelha" },
        };

        public static string CaminhoPrefab(int id) =>
            PastaResources + "/Criatura_" + id + ".prefab";

        public static string CaminhoModelo(string nome) => PastaModelos + "/" + nome + ".fbx";

        public static string CaminhoTextura(string nome, string mapa) =>
            PastaModelos + "/" + nome + "_" + mapa + ".png";

        /// <summary>Caminho do FBX de cada criatura gerada.</summary>
        public static IEnumerable<string> Modelos()
        {
            foreach (var receita in Receitas)
                yield return CaminhoModelo(receita.Nome);
        }

        [MenuItem("Jogo do Grupo/Mobs/Gerar criaturas")]
        public static void Gerar()
        {
            CriarPasta(PastaArte);
            CriarPasta(PastaResources);
            var relatorio = new StringBuilder();
            foreach (var receita in Receitas)
            {
                ConfigurarTexturas(receita);
                ConfigurarModelo(receita);
                var material = CriarMaterial(receita);
                var controller = CriarController(receita);
                CriarPrefab(receita, material, controller, relatorio);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // O modo batch nao mostra o console: o resumo fica na pasta temporaria.
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "gerador-mobs.txt"),
                relatorio.ToString()
            );
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }

        static void ConfigurarTexturas(Receita receita)
        {
            ConfigurarTextura(CaminhoTextura(receita.Nome, MapaCor), true);
            if (receita.TemBrilho)
                ConfigurarTextura(CaminhoTextura(receita.Nome, MapaBrilho), false);
            if (receita.TemEmissao)
                ConfigurarTextura(CaminhoTextura(receita.Nome, MapaEmissao), true);
        }

        static void ConfigurarTextura(string caminho, bool cor)
        {
            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Textura nao encontrada: " + caminho);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = cor;
            importer.alphaSource = cor
                ? TextureImporterAlphaSource.None
                : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = TamanhoMaximoTextura;
            importer.anisoLevel = 4;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        static void ConfigurarModelo(Receita receita)
        {
            var caminho = CaminhoModelo(receita.Nome);
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
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clipes = importer.defaultClipAnimations;
            foreach (var clipe in clipes)
            {
                // O Blender exporta "<Armadura>|<Acao>"; o clipe fica so com o nome da acao.
                var nome = clipe.takeName;
                var barra = nome.LastIndexOf('|');
                clipe.name = barra >= 0 ? nome.Substring(barra + 1) : nome;
                var repete = clipe.name == ClipeParado || clipe.name == ClipeAndar;
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

        static Material CriarMaterial(Receita receita)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Shader URP Lit nao encontrado.");
            var caminho = PastaArte + "/Criatura_" + receita.Id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }
            material.shader = shader;
            material.name = "Criatura_" + receita.Id;

            var cor = AssetDatabase.LoadAssetAtPath<Texture2D>(
                CaminhoTextura(receita.Nome, MapaCor)
            );
            material.SetTexture("_BaseMap", cor);
            material.mainTexture = cor;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", LisuraPelo);

            if (receita.TemBrilho)
            {
                // Com o mapa, o URP le o metal do canal R e multiplica a lisura do canal A.
                material.SetTexture(
                    "_MetallicGlossMap",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        CaminhoTextura(receita.Nome, MapaBrilho)
                    )
                );
                material.SetFloat("_Smoothness", 1f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                material.SetTexture("_MetallicGlossMap", null);
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (receita.TemEmissao)
            {
                material.SetTexture(
                    "_EmissionMap",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        CaminhoTextura(receita.Nome, MapaEmissao)
                    )
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

        static AnimatorController CriarController(Receita receita)
        {
            var modelo = CaminhoModelo(receita.Nome);
            var caminho = PastaArte + "/Criatura_" + receita.Id + ".controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho) != null)
                AssetDatabase.DeleteAsset(caminho);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            controller.AddParameter(ParametroMorrer, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(ParametroAtacar, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(ParametroAndando, AnimatorControllerParameterType.Bool);

            var maquina = controller.layers[0].stateMachine;
            var parado = maquina.AddState(ClipeParado, new Vector3(250f, 0f, 0f));
            parado.motion = ObterClipe(modelo, ClipeParado);
            maquina.defaultState = parado;
            var andar = maquina.AddState(ClipeAndar, new Vector3(500f, 0f, 0f));
            andar.motion = ObterClipe(modelo, ClipeAndar);
            var ataque = maquina.AddState(ClipeAtaque, new Vector3(375f, -120f, 0f));
            ataque.motion = ObterClipe(modelo, ClipeAtaque);
            var morte = maquina.AddState(ClipeMorte, new Vector3(375f, 140f, 0f));
            morte.motion = ObterClipe(modelo, ClipeMorte);

            var paraAndar = parado.AddTransition(andar);
            paraAndar.AddCondition(AnimatorConditionMode.If, 0, ParametroAndando);
            paraAndar.duration = TransicaoAndar;
            var paraParado = andar.AddTransition(parado);
            paraParado.AddCondition(AnimatorConditionMode.IfNot, 0, ParametroAndando);
            paraParado.duration = TransicaoAndar;

            // So ataca parado ou andando (nunca depois de morto).
            foreach (var origem in new[] { parado, andar })
            {
                var paraAtaque = origem.AddTransition(ataque);
                paraAtaque.AddCondition(AnimatorConditionMode.If, 0, ParametroAtacar);
                paraAtaque.duration = TransicaoAtaque;
            }
            var voltar = ataque.AddTransition(parado);
            voltar.hasExitTime = true;
            voltar.exitTime = 1f;
            voltar.duration = VoltaDoAtaque;

            var paraMorte = maquina.AddAnyStateTransition(morte);
            paraMorte.AddCondition(AnimatorConditionMode.If, 0, ParametroMorrer);
            paraMorte.duration = TransicaoMorte;
            paraMorte.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void CriarPrefab(
            Receita receita,
            Material material,
            AnimatorController controller,
            StringBuilder relatorio
        )
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo(receita.Nome));
            var raiz = new GameObject("Criatura_" + receita.Id);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                // O FBX ja vem em metros, olhando para +Z e com os pes em Y=0. A raiz fica em
                // escala 1: o servidor manda a escala da instancia (ApresentadorDeInimigos).
                instancia.name = "Modelo";
                instancia.transform.SetParent(raiz.transform, false);
                foreach (var r in instancia.GetComponentsInChildren<Renderer>())
                {
                    var materiais = new Material[r.sharedMaterials.Length];
                    for (var i = 0; i < materiais.Length; i++)
                        materiais[i] = material;
                    r.sharedMaterials = materiais;
                }

                var animator = instancia.GetComponent<Animator>();
                if (animator == null)
                    animator = instancia.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var limites = Limites(instancia);
                var cabeca = Procurar(instancia.transform, "Cabeca");
                relatorio.AppendLine(
                    receita.Id
                        + " "
                        + receita.Nome
                        + " limites="
                        + limites.min.ToString("F3")
                        + ".."
                        + limites.max.ToString("F3")
                        + " cabeca="
                        + (cabeca == null ? "?" : cabeca.position.ToString("F3"))
                );

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab(receita.Id));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        static Transform Procurar(Transform pai, string nome)
        {
            if (pai.name == nome)
                return pai;
            foreach (Transform filho in pai)
            {
                var achado = Procurar(filho, nome);
                if (achado != null)
                    return achado;
            }
            return null;
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
