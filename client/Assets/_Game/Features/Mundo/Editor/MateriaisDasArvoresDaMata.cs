using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// As árvores da Quaternius vêm com materiais embutidos no FBX sem recorte de alfa: no
    /// terreno as folhas apareciam como quadrados verdes com borda preta. Cria dois materiais URP
    /// (casca e folhas com recorte, dos dois lados, com instancing) e remapeia os materiais
    /// embutidos de cada árvore para eles. Roda junto com o <see cref="ReparoDoTerrenoPublicado"/>.
    /// </summary>
    public static class MateriaisDasArvoresDaMata
    {
        private const string PastaArvores =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Resources/Mundo/MataEstilizada/";
        private const string PastaTexturas =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Textures/";
        private const string PastaMateriais = "Assets/_Game/Features/Mundo/Art/Vegetacao/";

        private const float RecorteDasFolhas = 0.45f;

        [MenuItem("Jogo do Grupo/Mundo/Consertar materiais das árvores da mata")]
        public static void Consertar() => Consertar(new List<string>());

        public static void Consertar(List<string> relatorio)
        {
            var casca = Material(
                "CascaDaMata",
                PastaTexturas + "Bark_NormalTree.png",
                PastaTexturas + "Bark_NormalTree_Normal.png",
                folhas: false
            );
            var folhas = Material(
                "FolhasDaMata",
                PastaTexturas + "Leaves_NormalTree_C.png",
                null,
                folhas: true
            );
            var flores = Material(
                "FloresDaMata",
                PastaTexturas + "Flowers.png",
                null,
                folhas: true
            );
            if (casca == null || folhas == null || flores == null)
            {
                Debug.LogError("[ArvoresDaMata] Texturas da Quaternius não encontradas.");
                return;
            }

            // As árvores e os arbustos (que os pontos de coleta usam): todos com folhas
            // verdes da árvore comum; os arbustos vinham com a folha vermelha da árvore torta.
            var modelos = new List<string>();
            modelos.AddRange(
                AssetDatabase.FindAssets("CommonTree t:Model", new[] { PastaArvores })
            );
            modelos.AddRange(
                AssetDatabase.FindAssets("Bush_Common t:Model", new[] { PastaArvores })
            );
            foreach (var caminho in modelos)
            {
                var fbx = AssetDatabase.GUIDToAssetPath(caminho);
                var importador = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (importador == null)
                    continue;

                var mudou = false;
                foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(fbx))
                {
                    if (objeto is not Material embutido)
                        continue;
                    var destino =
                        embutido.name.StartsWith("Leaves") ? folhas
                        : embutido.name.StartsWith("Bark") ? casca
                        : embutido.name.StartsWith("Flowers") ? flores
                        : null;
                    if (destino == null)
                        continue;
                    importador.AddRemap(
                        new AssetImporter.SourceAssetIdentifier(typeof(Material), embutido.name),
                        destino
                    );
                    mudou = true;
                }

                if (mudou)
                {
                    importador.SaveAndReimport();
                    relatorio.Add(
                        "materiais de " + System.IO.Path.GetFileNameWithoutExtension(fbx)
                    );
                }
            }
        }

        private static Material Material(string nome, string textura, string normal, bool folhas)
        {
            var mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(textura);
            if (mapa == null)
                return null;

            if (!AssetDatabase.IsValidFolder(PastaMateriais.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Game/Features/Mundo/Art", "Vegetacao");

            var caminho = PastaMateriais + nome + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            var novo = material == null;
            if (novo)
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = nome,
                };

            material.SetTexture("_BaseMap", mapa);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", folhas ? 0.15f : 0.05f);
            if (normal != null)
            {
                var mapaNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
                if (mapaNormal != null)
                {
                    material.SetTexture("_BumpMap", mapaNormal);
                    material.EnableKeyword("_NORMALMAP");
                }
            }

            if (folhas)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", RecorteDasFolhas);
                material.EnableKeyword("_ALPHATEST_ON");
                // Dos dois lados: as folhas são planos finos.
                material.SetFloat("_Cull", 0f);
                material.doubleSidedGI = true;
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            material.enableInstancing = true;
            if (novo)
                AssetDatabase.CreateAsset(material, caminho);
            else
                EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
