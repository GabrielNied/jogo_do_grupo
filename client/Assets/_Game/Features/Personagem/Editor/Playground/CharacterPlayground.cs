using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Operacoes exclusivamente de Editor, restritas a cena de desenvolvimento.</summary>
    public static class CharacterPlayground
    {
        public const string Pasta = "Assets/_Game/Features/Personagem/Dev/CharacterPlayground";
        public const string Cena = Pasta + "/CharacterPlayground.unity";
        public const string Catalogo = Pasta + "/Personagens.asset";
        public const string PrefabInicial =
            "Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/HumanMaleHumanoid.prefab";
        public static readonly string[] Sockets =
        {
            "Weapon.R",
            "Costas",
            "EncaixeMaoDireita",
            "EncaixeCostas",
        };
        public static bool PodeEditar =>
            !EditorApplication.isPlayingOrWillChangePlaymode
            && SceneManager.GetActiveScene().path == Cena;
        public static CatalogoPlayground Lista =>
            AssetDatabase.LoadAssetAtPath<CatalogoPlayground>(Catalogo);
        public static bool PodeOperar =>
            SceneManager.GetActiveScene().path == Cena
            && !EditorApplication.isCompiling
            && (!EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying);

        public static Transform Raiz(string nome)
        {
            var cena = SceneManager.GetActiveScene();
            if (cena.path != Cena)
                return null;
            return cena.GetRootGameObjects().FirstOrDefault(o => o.name == nome)?.transform;
        }

        public static GameObject Atual =>
            Raiz("PersonagemAtual") is Transform raiz && raiz.childCount > 0
                ? Enumerable
                    .Range(0, raiz.childCount)
                    .Select(i => raiz.GetChild(i).gameObject)
                    .FirstOrDefault(o => o.activeSelf)
                : null;

        public static void Abrir()
        {
            AcessoCharacterPlayground.Abrir();
        }

        public static GameObject Spawn(int indice)
        {
            if (!PodeOperar)
                throw new InvalidOperationException(
                    "Abra a cena Character Playground fora do Play Mode."
                );
            var lista = Lista;
            if (
                lista == null
                || indice < 0
                || indice >= lista.Personagens.Count
                || lista.Personagens[indice]?.Prefab == null
            )
                throw new InvalidOperationException(
                    "Selecione uma entrada com prefab valido no catalogo."
                );
            var prefab = lista.Personagens[indice].Prefab;
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException(
                    "O catalogo deve referenciar um prefab do projeto."
                );
            var spawn = Raiz("Spawn Point");
            var container = Raiz("PersonagemAtual");
            if (spawn == null || container == null)
                throw new InvalidOperationException(
                    "Spawn Point ou PersonagemAtual ausente na cena."
                );
            if (Application.isPlaying)
                return SpawnRuntime(indice, prefab, spawn, container);
            RuntimeCharacterPlayground.Indice = indice;
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Spawn personagem no Playground");
            // Instanciar antes de destruir preserva o personagem anterior se o prefab falhar.
            var novo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
            Undo.RegisterCreatedObjectUndo(novo, "Spawn personagem");
            novo.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            for (var i = container.childCount - 1; i >= 0; i--)
                if (container.GetChild(i).gameObject != novo)
                    Undo.DestroyObjectImmediate(container.GetChild(i).gameObject);
            Selection.activeGameObject = novo;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(grupo);
            return novo;
        }

        static GameObject SpawnRuntime(
            int indice,
            GameObject prefab,
            Transform spawn,
            Transform container
        )
        {
            RuntimeCharacterPlayground.Liberar();
            var novo = UnityEngine.Object.Instantiate(prefab, container);
            novo.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            for (var i = container.childCount - 1; i >= 0; i--)
            {
                var anterior = container.GetChild(i).gameObject;
                if (anterior == novo)
                    continue;
                anterior.SetActive(false);
                UnityEngine.Object.Destroy(anterior);
            }
            RuntimeCharacterPlayground.Indice = indice;
            RuntimeCharacterPlayground.Conectar(novo);
            return novo;
        }

        public static void Reset(int indice)
        {
            Spawn(indice);
            Inspecionar(0, 1f);
        }

        public static Bounds Medir(GameObject objeto)
        {
            var limites = new Bounds();
            var iniciou = false;
            foreach (var renderer in objeto.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh);
                    foreach (var vertice in mesh.vertices)
                    {
                        var ponto = renderer.transform.TransformPoint(vertice);
                        if (!iniciou)
                        {
                            limites = new Bounds(ponto, Vector3.zero);
                            iniciou = true;
                        }
                        else
                            limites.Encapsulate(ponto);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
            if (!iniciou)
                foreach (var renderer in objeto.GetComponentsInChildren<Renderer>())
                    if (!iniciou)
                    {
                        limites = renderer.bounds;
                        iniciou = true;
                    }
                    else
                        limites.Encapsulate(renderer.bounds);
            return limites;
        }

        public static Transform Socket(GameObject objeto, string nome) =>
            objeto?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nome);

        public static void Inspecionar(int lado, float zoom)
        {
            if (!PodeEditar || Atual == null)
                return;
            var camera = Raiz("Camera de inspecao")?.GetComponent<Camera>();
            if (camera == null)
                return;
            var bounds = Medir(Atual);
            var rotacao = Quaternion.Euler(5, lado * 90 + 180, 0);
            var distancia =
                Mathf.Max(bounds.size.y, bounds.size.x) * 1.8f / Mathf.Clamp(zoom, 0.5f, 2f);
            Undo.RecordObject(camera.transform, "Inspecionar personagem");
            camera.transform.SetPositionAndRotation(
                bounds.center - rotacao * Vector3.forward * distancia,
                rotacao
            );
            camera.nearClipPlane = 0.05f;
            var vista = SceneView.lastActiveSceneView;
            if (vista != null)
                vista.LookAt(bounds.center, rotacao, distancia * 0.5f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Jogo do Grupo/Personagem/Character Playground/Gerar cena inicial")]
        public static void Gerar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Cena) != null)
            {
                Abrir();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            System.IO.Directory.CreateDirectory(Pasta);
            AssetDatabase.Refresh();
            var lista = ScriptableObject.CreateInstance<CatalogoPlayground>();
            lista.Personagens.Add(
                new CatalogoPlayground.Personagem
                {
                    Nome = "Human Male",
                    Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabInicial),
                }
            );
            AssetDatabase.CreateAsset(lista, Catalogo);
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Spawn Point");
            new GameObject("PersonagemAtual");
            var ambiente = new GameObject("Ambiente metrico");
            var chao = Material("Chao", new Color(0.23f, 0.27f, 0.29f));
            var marca = Material("Marcacoes", new Color(0.38f, 0.47f, 0.48f));
            var referencia = Material("Referencias", new Color(0.66f, 0.54f, 0.31f));
            Cubo(
                "Chao 12 x 12 m",
                new Vector3(0, -0.05f, 0),
                new Vector3(12, 0.1f, 12),
                chao,
                ambiente.transform
            );
            for (var i = -6; i <= 6; i++)
            {
                Cubo(
                    "Grid 1 m X",
                    new Vector3(i, 0.002f, 0),
                    new Vector3(0.012f, 0.002f, 12),
                    marca,
                    ambiente.transform
                );
                Cubo(
                    "Grid 1 m Z",
                    new Vector3(0, 0.002f, i),
                    new Vector3(12, 0.002f, 0.012f),
                    marca,
                    ambiente.transform
                );
            }
            Cubo(
                "Referencia 2 m",
                new Vector3(-2, 1, 0),
                new Vector3(0.06f, 2, 0.06f),
                referencia,
                ambiente.transform
            );
            for (var i = 1; i <= 4; i++)
                Cubo(
                    "Marca altura " + i * 0.5f + " m",
                    new Vector3(-2, i * 0.5f, 0),
                    new Vector3(0.25f, 0.015f, 0.06f),
                    referencia,
                    ambiente.transform
                );
            Cubo(
                "Referencia comprimento 1 m",
                new Vector3(2, 0.55f, 0),
                new Vector3(1, 0.035f, 0.035f),
                referencia,
                ambiente.transform
            );
            Cubo(
                "Target Test Area",
                new Vector3(0, 0.004f, 4),
                new Vector3(2, 0.004f, 2),
                referencia,
                ambiente.transform
            );
            var camera = new GameObject("Camera de inspecao").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.18f);
            var luz = new GameObject("Luz principal").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.8f;
            luz.transform.rotation = Quaternion.Euler(45, -30, 0);
            var preenchimento = new GameObject("Luz de preenchimento").AddComponent<Light>();
            preenchimento.type = LightType.Directional;
            preenchimento.intensity = 0.6f;
            preenchimento.transform.rotation = Quaternion.Euler(25, 150, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            EditorSceneManager.SaveScene(cena, Cena);
            Reset(0);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
        }

        static Material Material(string nome, Color cor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = nome,
            };
            material.SetColor("_BaseColor", cor);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, Pasta + "/" + nome + ".mat");
            return material;
        }

        static void Cubo(
            string nome,
            Vector3 posicao,
            Vector3 escala,
            Material material,
            Transform pai
        )
        {
            var objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objeto.name = nome;
            objeto.transform.SetParent(pai);
            objeto.transform.position = posicao;
            objeto.transform.localScale = escala;
            objeto.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(objeto.GetComponent<Collider>());
        }
    }
}
