using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Reune camadas prontas sem mover os pacotes nem alterar a pintura existente.</summary>
    public static class OrganizadorDeCamadasDeTerreno
    {
        public const string Pasta = "Assets/_Game/Features/Mundo/Art/CamadasDeTerreno";

        // Tambem atende pacotes importados antes de instalar esta ferramenta.
        [InitializeOnLoadMethod]
        private static void AoCarregar()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += ReunirAoCarregar;
        }

        private static void ReunirAoCarregar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            Reunir();
        }

        [MenuItem("Jogo do Grupo/Mundo/Texturas do terreno/Reunir e abrir pasta")]
        public static void AbrirPasta()
        {
            var camadas = Reunir();
            if (camadas.Count == 0)
            {
                Debug.LogWarning("Nenhuma camada de terreno com textura foi encontrada em Assets.");
                return;
            }
            var pasta = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Pasta);
            Selection.activeObject = pasta;
            EditorGUIUtility.PingObject(pasta);
            Debug.Log($"{camadas.Count} camadas de terreno disponiveis em {Pasta}.");
        }

        [MenuItem("Jogo do Grupo/Mundo/Texturas do terreno/Adicionar ao terreno selecionado")]
        public static void AdicionarAoTerreno()
        {
            var terreno =
                Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<UnityEngine.Terrain>()
                    : null;
            if (terreno == null || terreno.terrainData == null)
            {
                EditorUtility.DisplayDialog(
                    "Texturas do terreno",
                    "Selecione na Hierarchy o objeto que tem o componente Terrain e tente novamente.",
                    "OK"
                );
                return;
            }

            var dados = terreno.terrainData;
            var camadas = new List<TerrainLayer>(dados.terrainLayers);
            var quantidadeAnterior = camadas.Count;
            foreach (var par in Reunir())
            {
                if (!camadas.Contains(par.Key) && !camadas.Contains(par.Value))
                    camadas.Add(par.Value);
            }
            if (camadas.Count == quantidadeAnterior)
            {
                Debug.Log("O terreno ja tem todas as camadas reunidas.", terreno);
                return;
            }

            // Os indices antigos continuam iguais: nao substitui nem reordena a pintura.
            Undo.RegisterCompleteObjectUndo(dados, "Adicionar texturas ao terreno");
            dados.terrainLayers = camadas.ToArray();
            EditorUtility.SetDirty(dados);
            terreno.Flush();
            Debug.Log(
                $"Adicionadas {camadas.Count - quantidadeAnterior} camadas. A pintura foi preservada. Ctrl+Z desfaz.",
                terreno
            );
        }

        private static List<KeyValuePair<TerrainLayer, TerrainLayer>> Reunir()
        {
            var resultado = new List<KeyValuePair<TerrainLayer, TerrainLayer>>();
            var guids = AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets" });
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (var guid in guids)
            {
                var origem = AssetDatabase.GUIDToAssetPath(guid);
                if (origem.StartsWith(Pasta + "/", StringComparison.Ordinal))
                    continue;
                var camada = AssetDatabase.LoadAssetAtPath<TerrainLayer>(origem);
                if (camada == null || camada.diffuseTexture == null)
                    continue;

                var pacote = origem.Split('/')[1];
                var destinoPasta = Pasta + "/" + pacote;
                CriarPastas(destinoPasta);
                // GUID completo impede colisoes e permite repetir sem duplicar camadas.
                var destino =
                    destinoPasta
                    + "/"
                    + Path.GetFileNameWithoutExtension(origem)
                    + "_"
                    + guid
                    + ".terrainlayer";
                var copia = AssetDatabase.LoadAssetAtPath<TerrainLayer>(destino);
                if (copia == null)
                {
                    if (!AssetDatabase.CopyAsset(origem, destino))
                    {
                        Debug.LogError($"Nao foi possivel reunir a camada {origem}.");
                        continue;
                    }
                    copia = AssetDatabase.LoadAssetAtPath<TerrainLayer>(destino);
                }
                if (copia != null)
                    resultado.Add(new KeyValuePair<TerrainLayer, TerrainLayer>(camada, copia));
            }
            return resultado;
        }

        private static void CriarPastas(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var separador = caminho.LastIndexOf('/');
            var pai = caminho.Substring(0, separador);
            CriarPastas(pai);
            AssetDatabase.CreateFolder(pai, caminho.Substring(separador + 1));
        }
    }
}
