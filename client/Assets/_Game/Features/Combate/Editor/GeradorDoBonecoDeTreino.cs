using System.IO;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Editor
{
    /// <summary>
    /// Gera o prefab do boneco de treino de palha (#1188) a partir do <c>Dummy.fbx</c> CC0 do
    /// Fantasy Props MegaKit (Quaternius): materiais URP com as três texturas de cor do pacote, a
    /// altura de <see cref="RegrasBonecoDeTreino.Altura"/>, a base no zero e sem colisor. Reproduzível:
    /// rodar de novo regrava o prefab e os materiais. Uso (editor fechado):
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Combate.Editor.GeradorDoBonecoDeTreino.Gerar</c>.
    /// </summary>
    public static class GeradorDoBonecoDeTreino
    {
        private const string Fbx = "Assets/ThirdParty/Quaternius/FantasyPropsMegaKit/Dummy.fbx";
        private const string Texturas =
            "Assets/ThirdParty/Quaternius/FantasyPropsMegaKit/Textures/";
        private const string Pasta = "Assets/_Game/Features/Combate/Resources/Combate";
        private const string Prefab = Pasta + "/BonecoDeTreino.prefab";

        // Nome do material no FBX e a textura de cor dele.
        private static readonly (string Material, string Textura)[] Placas =
        {
            ("MI_Trim_Furniture", "T_Trim_Furniture_BaseColor.png"),
            ("MI_Trim_Metal", "T_Trim_Metal_BaseColor.png"),
            ("MI_Trim_Cloth", "T_Trim_Cloth_BaseColor.png"),
        };

        public static void Gerar()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (fbx == null)
            {
                throw new FileNotFoundException("FBX ausente: " + Fbx);
            }

            Directory.CreateDirectory(Pasta);
            AssetDatabase.Refresh();

            var materiais = new Material[Placas.Length];
            for (var i = 0; i < Placas.Length; i++)
            {
                materiais[i] = MaterialDaPlaca(Placas[i].Material, Placas[i].Textura);
            }

            var raiz = new GameObject("BonecoDeTreino");
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            instancia.name = "Modelo";
            instancia.transform.SetParent(raiz.transform, false);
            // O FBX do pacote tem a altura no eixo Z: deita -90 em X para ficar em pé e depois gira
            // em Y (GiroDoModelo) para a frente ficar em +Z.
            instancia.transform.localRotation =
                Quaternion.Euler(0f, GiroDoModelo, 0f) * Quaternion.Euler(-90f, 0f, 0f);

            foreach (var colisor in raiz.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(colisor);
            }

            foreach (var renderizador in raiz.GetComponentsInChildren<Renderer>())
            {
                var atuais = renderizador.sharedMaterials;
                for (var i = 0; i < atuais.Length; i++)
                {
                    var nome = atuais[i] != null ? atuais[i].name : "";
                    for (var p = 0; p < Placas.Length; p++)
                    {
                        if (nome.StartsWith(Placas[p].Material))
                        {
                            atuais[i] = materiais[p];
                        }
                    }
                }

                renderizador.sharedMaterials = atuais;
            }

            var limites = Limites(raiz);
            var escala = (float)RegrasBonecoDeTreino.Altura / limites.size.y;
            instancia.transform.localScale *= escala;
            limites = Limites(raiz);
            instancia.transform.localPosition -= new Vector3(
                limites.center.x,
                limites.min.y,
                limites.center.z
            );

            PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);
            Object.DestroyImmediate(raiz);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Combate] Boneco de treino gerado em " + Prefab + ", escala " + escala);
        }

        /// <summary>
        /// Foto do boneco de frente, de lado e de costas em <c>BONECO_SAIDA</c> (padrão <c>prints/</c>),
        /// para conferir o modelo e as texturas sem abrir o editor. Uso: <c>npm run unity:executar --
        /// JogoDoGrupo.Client.Features.Combate.Editor.GeradorDoBonecoDeTreino.Capturar</c>.
        /// </summary>
        public static void Capturar()
        {
            var saida = System.Environment.GetEnvironmentVariable("BONECO_SAIDA") ?? "prints";
            Directory.CreateDirectory(saida);
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var boneco = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.3f, 0.35f, 0.4f);
            try
            {
                var nomes = new[] { "frente", "lado", "costas" };
                var giros = new[] { 0f, 90f, 180f };
                for (var i = 0; i < nomes.Length; i++)
                {
                    var centro = boneco.transform.position + Vector3.up * 1f;
                    camera.transform.position =
                        centro + Quaternion.Euler(0f, giros[i], 0f) * Vector3.forward * 3.2f;
                    camera.transform.LookAt(centro);
                    var textura = new RenderTexture(600, 600, 24);
                    camera.targetTexture = textura;
                    camera.Render();
                    RenderTexture.active = textura;
                    var imagem = new Texture2D(600, 600, TextureFormat.RGB24, false);
                    imagem.ReadPixels(new Rect(0, 0, 600, 600), 0, 0);
                    File.WriteAllBytes(
                        Path.Combine(saida, "boneco-" + nomes[i] + ".png"),
                        imagem.EncodeToPNG()
                    );
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    Object.DestroyImmediate(textura);
                    Object.DestroyImmediate(imagem);
                }

                Debug.Log("[Combate] Fotos do boneco em " + Path.GetFullPath(saida));
            }
            finally
            {
                Object.DestroyImmediate(boneco);
                Object.DestroyImmediate(luz.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        /// <summary>Giro do FBX em Y para a frente do boneco ficar em +Z.</summary>
        private const float GiroDoModelo = 0f;

        private static Material MaterialDaPlaca(string nome, string textura)
        {
            var caminho = Pasta + "/" + nome + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, caminho);
            }

            var mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + textura);
            if (mapa == null)
            {
                throw new FileNotFoundException("Textura ausente: " + Texturas + textura);
            }

            material.SetTexture("_BaseMap", mapa);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Bounds Limites(GameObject objeto)
        {
            var achou = false;
            var limites = new Bounds();
            foreach (var renderizador in objeto.GetComponentsInChildren<Renderer>())
            {
                if (!achou)
                {
                    limites = renderizador.bounds;
                    achou = true;
                }
                else
                {
                    limites.Encapsulate(renderizador.bounds);
                }
            }

            return limites;
        }
    }
}
