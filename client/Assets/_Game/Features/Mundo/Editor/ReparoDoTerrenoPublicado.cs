using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Conserta o <c>TerrenoDoMundo.asset</c> publicado (#1172, #1179): camadas de terreno sem
    /// textura (ou que não existem no repositório) viram xadrez cinza no jogo, e protótipos de
    /// árvore sem prefab enchem o Console de "Tree prefab at index N is missing". Troca só as
    /// referências quebradas por assets que estão no repositório, sem mexer em alturas, pesos das
    /// camadas, buracos, detalhes nem na posição das árvores já pintadas.
    /// <para>
    /// Linha de comando:
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.ReparoDoTerrenoPublicado.Reparar</c>.
    /// </para>
    /// </summary>
    public static class ReparoDoTerrenoPublicado
    {
        public const string CaminhoTerreno =
            "Assets/_Game/Features/Mundo/Resources/Mundo/TerrenoDoMundo.asset";

        private const string PastaCamadas =
            "Assets/3D Haven/Free Fantasy Terrain Textures/Demo/Terrains/Terrain Layers/";

        // Camadas de reserva, na ordem de preferência para cada uso.
        private static readonly string[] CamadaDaCidade =
        {
            PastaCamadas + "3DH FTT Path_001 2K.terrainlayer",
        };
        private static readonly string[] CamadaDeEstrada =
        {
            PastaCamadas + "3DH FTT Dirt_002a 2K.terrainlayer",
        };

        private const string PastaArvores =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Resources/Mundo/MataEstilizada/";

        private static readonly string[] Arvores =
        {
            PastaArvores + "CommonTree_1.fbx",
            PastaArvores + "CommonTree_2.fbx",
            PastaArvores + "CommonTree_3.fbx",
            PastaArvores + "CommonTree_4.fbx",
            PastaArvores + "CommonTree_5.fbx",
        };

        [MenuItem("Jogo do Grupo/Mundo/Reparar terreno publicado")]
        public static void Reparar()
        {
            var dados = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoTerreno);
            if (dados == null)
            {
                Debug.LogError("[ReparoDoTerreno] TerrainData não encontrado: " + CaminhoTerreno);
                return;
            }

            var relatorio = new List<string>();
            RepararCamadas(dados, relatorio);
            RepararArvores(dados, relatorio);
            MateriaisDasArvoresDaMata.Consertar(relatorio);

            if (relatorio.Count == 0)
            {
                Debug.Log("[ReparoDoTerreno] Nada a consertar.");
                return;
            }

            EditorUtility.SetDirty(dados);
            AssetDatabase.SaveAssets();
            Debug.Log("[ReparoDoTerreno] " + string.Join("; ", relatorio));
        }

        private static void RepararCamadas(TerrainData dados, List<string> relatorio)
        {
            var camadas = dados.terrainLayers;
            var mudou = false;
            for (var i = 0; i < camadas.Length; i++)
            {
                var camada = camadas[i];
                if (camada != null && camada.diffuseTexture != null)
                    continue;

                // A camada de pedras foi pintada só na praça da cidade; a outra quebrada, na
                // estrada e no platô a noroeste. Pelo nome dá para saber qual é qual.
                var ehCidade = camada != null && camada.name.Contains("stone");
                var reserva = Carregar<TerrainLayer>(ehCidade ? CamadaDaCidade : CamadaDeEstrada);
                if (reserva == null)
                    continue;

                relatorio.Add(
                    "camada "
                        + i
                        + " ("
                        + (camada != null ? camada.name : "ausente")
                        + ") → "
                        + reserva.name
                );
                camadas[i] = reserva;
                mudou = true;
            }

            if (mudou)
                dados.terrainLayers = camadas;
        }

        private static void RepararArvores(TerrainData dados, List<string> relatorio)
        {
            var prototipos = dados.treePrototypes;
            var mudou = false;
            for (var i = 0; i < prototipos.Length; i++)
            {
                if (prototipos[i].prefab != null)
                    continue;

                var arvore = AssetDatabase.LoadAssetAtPath<GameObject>(Arvores[i % Arvores.Length]);
                if (arvore == null)
                    continue;

                prototipos[i].prefab = arvore;
                relatorio.Add("árvore " + i + " → " + arvore.name);
                mudou = true;
            }

            if (!mudou)
                return;

            // Guardar as instâncias antes: trocar os protótipos não pode apagar as árvores pintadas.
            var instancias = dados.treeInstances;
            dados.treePrototypes = prototipos;
            dados.RefreshPrototypes();
            dados.SetTreeInstances(instancias, true);
        }

        private static T Carregar<T>(string[] caminhos)
            where T : Object
        {
            foreach (var caminho in caminhos)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(caminho);
                if (asset != null)
                    return asset;
            }

            return null;
        }
    }
}
