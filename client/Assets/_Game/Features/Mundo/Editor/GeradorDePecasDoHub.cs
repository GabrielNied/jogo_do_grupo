using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Gera os prefabs do hub em <c>Features/Mundo/Resources/Mundo/</c> a partir dos modelos
    /// Kenney "Fantasy Town Kit" (CC0) de <c>ThirdParty/</c>: escala para a grade de 2 m,
    /// material de cores (URP Lit) e colisores. Reproduzível: rodar de novo regenera tudo.
    /// Com o editor fechado: <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.GeradorDePecasDoHub.Gerar</c>.
    /// </summary>
    public static class GeradorDePecasDoHub
    {
        private const string PastaKenney = "Assets/ThirdParty/Kenney/FantasyTownKit";
        private const string PastaPecas = "Assets/_Game/Features/Mundo/Resources/Mundo";
        private const string PastaMateriais = PastaPecas + "/Materiais";

        // Medida da grade: as peças Kenney têm 1 m de lado; o hub usa o dobro.
        private const float M = 2f;

        private enum Colisor
        {
            Nenhum,
            Caixa,
            Capsula,
        }

        [MenuItem("Jogo do Grupo/Mundo/Gerar peças do hub")]
        public static void Gerar()
        {
            Directory.CreateDirectory(PastaMateriais);
            AssetDatabase.Refresh();

            ConfigurarTextura();
            var cores = CriarMaterial("Kenney_Colormap", Color.white, CarregarTextura());
            var grama = CriarMaterial("Grama", new Color(0.42f, 0.6f, 0.3f), null);

            CriarChao(grama);

            CriarPeca("Muro_Bloco", "wall-block", new Vector3(M, 3f, M), Colisor.Caixa, cores);
            CriarPeca("Torre_Canto", "wall-block", new Vector3(M, 6f, M), Colisor.Caixa, cores);
            CriarPeca("Estrada", "road", new Vector3(M, M, M), Colisor.Nenhum, cores);
            CriarPeca("Fonte", "fountain-round", new Vector3(M, M, M), Colisor.Caixa, cores);
            CriarPeca("Banco", "stall-bench", new Vector3(M, M, M), Colisor.Caixa, cores);
            CriarPeca("Barraca_Vermelha", "stall-red", new Vector3(M, M, M), Colisor.Caixa, cores);
            CriarPeca("Barraca_Verde", "stall-green", new Vector3(M, M, M), Colisor.Caixa, cores);
            CriarPeca("Carroca", "cart", new Vector3(M, M, M), Colisor.Caixa, cores);
            CriarPeca("Pedra_Grande", "rock-large", Vector3.one * 1.5f, Colisor.Caixa, cores);
            CriarPeca("Pedra_Larga", "rock-wide", Vector3.one * 1.5f, Colisor.Caixa, cores);
            CriarPeca("Pedra_Pequena", "rock-small", Vector3.one * M, Colisor.Caixa, cores);

            // Troncos e postes: cápsula fina em vez da caixa da copa/da lâmpada.
            CriarPeca("Arvore", "tree", Vector3.one * M, Colisor.Capsula, cores, 0.45f, 1.5f);
            CriarPeca(
                "Arvore_Alta",
                "tree-high",
                Vector3.one * M,
                Colisor.Capsula,
                cores,
                0.45f,
                2f
            );
            CriarPeca(
                "Lanterna",
                "lantern",
                Vector3.one * M,
                Colisor.Capsula,
                cores,
                0.2f,
                3.1f,
                true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mundo] Peças do hub geradas em " + PastaPecas);
        }

        private static Texture2D CarregarTextura()
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(PastaKenney + "/Textures/colormap.png");
        }

        // A paleta tem blocos de cor lisos: sem mipmap e sem filtro, para uma cor não vazar na
        // vizinha.
        private static void ConfigurarTextura()
        {
            var caminho = PastaKenney + "/Textures/colormap.png";
            if (AssetImporter.GetAtPath(caminho) is TextureImporter importador)
            {
                if (importador.filterMode != FilterMode.Point || importador.mipmapEnabled)
                {
                    importador.filterMode = FilterMode.Point;
                    importador.mipmapEnabled = false;
                    importador.SaveAndReimport();
                }
            }
        }

        private static Material CriarMaterial(string nome, Color cor, Texture2D textura)
        {
            var caminho = $"{PastaMateriais}/{nome}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Shader URP/Lit não encontrado.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }

            material.SetColor("_BaseColor", cor);
            if (textura != null)
            {
                material.SetTexture("_BaseMap", textura);
            }

            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Chão de 40 x 40 m com a face de cima em Y = 0 (cubo de 1 m de espessura abaixo).
        private static void CriarChao(Material material)
        {
            var raiz = new GameObject("Chao_Hub");
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Visual";
            cubo.transform.SetParent(raiz.transform, false);
            cubo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            cubo.transform.localScale = new Vector3(40f, 1f, 40f);
            cubo.GetComponent<Renderer>().sharedMaterial = material;
            Salvar(raiz, "Chao_Hub");
        }

        private static void CriarPeca(
            string nome,
            string modelo,
            Vector3 escala,
            Colisor colisor,
            Material material,
            float raio = 0f,
            float altura = 0f,
            bool comLuz = false
        )
        {
            var prefabModelo = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PastaKenney}/{modelo}.fbx"
            );
            if (prefabModelo == null)
            {
                throw new FileNotFoundException($"Modelo '{modelo}.fbx' não encontrado.");
            }

            var raiz = new GameObject(nome);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefabModelo);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localScale = escala;

            var renderizadores = visual.GetComponentsInChildren<Renderer>();
            var limites = new Bounds();
            for (var i = 0; i < renderizadores.Length; i++)
            {
                renderizadores[i].sharedMaterial = material;
                if (i == 0)
                {
                    limites = renderizadores[i].bounds;
                }
                else
                {
                    limites.Encapsulate(renderizadores[i].bounds);
                }
            }

            if (colisor == Colisor.Caixa)
            {
                var caixa = raiz.AddComponent<BoxCollider>();
                caixa.center = limites.center;
                caixa.size = limites.size;
            }
            else if (colisor == Colisor.Capsula)
            {
                var capsula = raiz.AddComponent<CapsuleCollider>();
                capsula.radius = raio;
                capsula.height = altura;
                capsula.center = new Vector3(0f, altura / 2f, 0f);
            }

            if (comLuz)
            {
                CriarLuzDaLanterna(raiz.transform, limites.max.y);
            }

            Debug.Log($"[Mundo] {nome}: tamanho {limites.size}, base em y={limites.min.y:0.00}");
            Salvar(raiz, nome);
        }

        private static void CriarLuzDaLanterna(Transform pai, float topo)
        {
            var objeto = new GameObject("Luz");
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(0f, topo - 0.3f, 0f);

            var luz = objeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.74f, 0.42f);
            luz.intensity = 3f;
            luz.range = 8f;
            luz.shadows = LightShadows.None;
        }

        private static void Salvar(GameObject raiz, string nome)
        {
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{PastaPecas}/{nome}.prefab");
            UnityEngine.Object.DestroyImmediate(raiz);
        }
    }
}
