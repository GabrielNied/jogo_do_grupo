using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Acessa as camadas originais sem copiar ou mover assets dos pacotes.</summary>
    public static class OrganizadorDeCamadasDeTerreno
    {
        public const string Pasta = "Assets/_Game/Features/Mundo/Art/CamadasDeTerreno";

        [MenuItem("Jogo do Grupo/Mundo/Texturas do terreno/Selecionar camadas disponiveis")]
        public static void AbrirPasta()
        {
            Selection.objects = Originais().Cast<UnityEngine.Object>().ToArray();
            EditorUtility.FocusProjectWindow();
            Debug.Log("Camadas originais selecionadas. Nenhum arquivo foi copiado ou movido.");
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
                Debug.LogWarning("Selecione o objeto ChaoNativo (Terrain) na Hierarchy.");
                return;
            }
            var dados = terreno.terrainData;
            var camadas = new List<TerrainLayer>(dados.terrainLayers);
            // Uma copia antiga ja presente equivale a sua origem: nao acrescentar outra.
            var presentes = new HashSet<TerrainLayer>(camadas.Select(OrigemOuPropria));
            foreach (var camada in Originais())
                if (presentes.Add(camada))
                    camadas.Add(camada);
            if (camadas.Count == dados.terrainLayers.Length)
                return;
            Undo.RegisterCompleteObjectUndo(dados, "Adicionar texturas ao terreno");
            dados.terrainLayers = camadas.ToArray();
            EditorUtility.SetDirty(dados);
            terreno.Flush();
        }

        private static TerrainLayer[] Originais()
        {
            return AssetDatabase
                .FindAssets("t:TerrainLayer", new[] { "Assets" })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(caminho => !caminho.StartsWith(Pasta + "/", StringComparison.Ordinal))
                .OrderBy(caminho => caminho, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>)
                .Where(camada => camada != null && camada.diffuseTexture != null)
                .ToArray();
        }

        private static TerrainLayer OrigemOuPropria(TerrainLayer camada)
        {
            var caminho = AssetDatabase.GetAssetPath(camada);
            if (!caminho.StartsWith(Pasta + "/", StringComparison.Ordinal))
                return camada;
            var nome = Path.GetFileNameWithoutExtension(caminho);
            if (nome.Length < 33 || nome[nome.Length - 33] != '_')
                return camada;
            var guid = nome.Substring(nome.Length - 32);
            if (!guid.All(Uri.IsHexDigit))
                return camada;
            var origem = AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                AssetDatabase.GUIDToAssetPath(guid)
            );
            return origem != null && origem != camada ? origem : camada;
        }

        [MenuItem(
            "Jogo do Grupo/Mundo/Texturas do terreno/Corrigir copias criadas pelo organizador"
        )]
        public static void CorrigirCopias()
        {
            if (
                !AssetDatabase.IsValidFolder(Pasta)
                || EditorApplication.isPlayingOrWillChangePlaymode
            )
                return;
            var substituicoes = new Dictionary<TerrainLayer, TerrainLayer>();
            foreach (var guid in AssetDatabase.FindAssets("t:TerrainLayer", new[] { Pasta }))
            {
                var copia = AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                var origem = OrigemOuPropria(copia);
                if (origem != copia && IguaisSemNome(copia, origem))
                    substituicoes.Add(copia, origem);
            }
            if (substituicoes.Count == 0)
            {
                Debug.Log(
                    "Nenhuma copia intacta para corrigir. Copias com ajustes proprios sao preservadas."
                );
                return;
            }

            // Backup fora de Assets antes de alterar ou remover qualquer asset.
            var backup = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../Library/BackupCamadasDeTerreno/" + DateTime.UtcNow.Ticks
                )
            );
            foreach (var copia in substituicoes.Keys)
                GuardarBackup(AssetDatabase.GetAssetPath(copia), backup);
            foreach (var guid in AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets" }))
            {
                var caminho = AssetDatabase.GUIDToAssetPath(guid);
                var dados = AssetDatabase.LoadAssetAtPath<TerrainData>(caminho);
                if (
                    !dados.terrainLayers.Any(camada =>
                        camada != null && substituicoes.ContainsKey(camada)
                    )
                )
                    continue;
                // O backup precisa incluir a pintura ainda nao salva pelo usuario.
                AssetDatabase.SaveAssetIfDirty(dados);
                GuardarBackup(caminho, backup);
                Consolidar(dados, substituicoes);
                EditorUtility.SetDirty(dados);
                AssetDatabase.SaveAssetIfDirty(dados);
            }

            // Nao remove copias ainda referenciadas por outro tipo de asset ou cena aberta.
            var caminhos = new HashSet<string>(
                substituicoes.Keys.Select(AssetDatabase.GetAssetPath)
            );
            var referenciadas = new HashSet<string>();
            foreach (var caminho in AssetDatabase.GetAllAssetPaths())
            {
                if (
                    !caminho.StartsWith("Assets/", StringComparison.Ordinal)
                    || AssetDatabase.IsValidFolder(caminho)
                )
                    continue;
                foreach (var dependencia in AssetDatabase.GetDependencies(caminho, false))
                    if (dependencia != caminho && caminhos.Contains(dependencia))
                        referenciadas.Add(dependencia);
            }
            foreach (
                var terreno in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(
                    FindObjectsInactive.Include
                )
            )
                if (terreno.terrainData != null)
                    foreach (var camada in terreno.terrainData.terrainLayers)
                        if (camada != null)
                            referenciadas.Add(AssetDatabase.GetAssetPath(camada));
            var removidas = 0;
            foreach (var caminho in caminhos)
                if (!referenciadas.Contains(caminho) && AssetDatabase.DeleteAsset(caminho))
                    removidas++;
            Debug.Log(
                $"{removidas} copias removidas; pintura consolidada nas originais. Backup: {backup}. Copias editadas ou ainda referenciadas foram preservadas."
            );
        }

        private static bool IguaisSemNome(TerrainLayer copia, TerrainLayer origem)
        {
            var comparacao = UnityEngine.Object.Instantiate(copia);
            try
            {
                comparacao.name = origem.name;
                comparacao.hideFlags = origem.hideFlags;
                return EditorJsonUtility.ToJson(comparacao) == EditorJsonUtility.ToJson(origem);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(comparacao);
            }
        }

        public static void Consolidar(
            TerrainData dados,
            Dictionary<TerrainLayer, TerrainLayer> substituicoes
        )
        {
            var antigas = dados.terrainLayers;
            if (!antigas.Any(camada => camada != null && substituicoes.ContainsKey(camada)))
                return;
            var novas = new List<TerrainLayer>();
            var indices = new int[antigas.Length];
            for (var i = 0; i < antigas.Length; i++)
            {
                var camada = antigas[i];
                if (camada != null && substituicoes.TryGetValue(camada, out var origem))
                    camada = origem;
                var indice = novas.IndexOf(camada);
                if (indice < 0)
                {
                    indice = novas.Count;
                    novas.Add(camada);
                }
                indices[i] = indice;
            }
            var pesos = dados.GetAlphamaps(0, 0, dados.alphamapWidth, dados.alphamapHeight);
            var novosPesos = new float[pesos.GetLength(0), pesos.GetLength(1), novas.Count];
            for (var z = 0; z < pesos.GetLength(0); z++)
            for (var x = 0; x < pesos.GetLength(1); x++)
            for (var camada = 0; camada < antigas.Length; camada++)
                novosPesos[z, x, indices[camada]] += pesos[z, x, camada];
            dados.terrainLayers = novas.ToArray();
            dados.SetAlphamaps(0, 0, novosPesos);
        }

        private static void GuardarBackup(string caminho, string backup)
        {
            var destino = Path.Combine(backup, caminho);
            var origem = Path.Combine(Path.GetDirectoryName(Application.dataPath), caminho);
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            File.Copy(origem, destino, true);
            if (File.Exists(origem + ".meta"))
                File.Copy(origem + ".meta", destino + ".meta", true);
        }
    }
}
