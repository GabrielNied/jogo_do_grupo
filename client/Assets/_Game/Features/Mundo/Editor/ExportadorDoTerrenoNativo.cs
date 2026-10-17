using System;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Transfere o relevo determinista atual para um TerrainData editável no Unity. O asset é uma
    /// cópia inicial: editar o Terrain não altera o mapa compartilhado nem o servidor.
    /// </summary>
    public static class ExportadorDoTerrenoNativo
    {
        private const string CaminhoDoTerrain =
            "Assets/_Game/Features/Mundo/Resources/Mundo/TerrenoDoMundo.asset";
        private const string CaminhoDaTexturaDaGrama =
            "Assets/ThirdParty/PolyHaven/Resources/Terreno/Grama.jpg";
        private const string NomeDaPrevia = "Terreno nativo (prévia)";
        private const int ResolucaoDoHeightmap = 1025;

        [MenuItem("Jogo do Grupo/Mundo/Gerar Terrain nativo a partir do mapa")]
        public static void Gerar()
        {
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoDoTerrain);
            if (terrain == null)
            {
                terrain = new TerrainData();
                AssetDatabase.CreateAsset(terrain, CaminhoDoTerrain);
            }

            terrain.heightmapResolution = ResolucaoDoHeightmap;
            terrain.size = new Vector3(
                MapaDoMundo.Lado,
                MapaDoMundo.AlturaMaxima - MapaDoMundo.AlturaMinima,
                MapaDoMundo.Lado
            );
            terrain.SetHeights(0, 0, AlturasNormalizadas());
            PrepararCamadaDoSolo(terrain);
            terrain.detailPrototypes = Array.Empty<DetailPrototype>();
            EditorUtility.SetDirty(terrain);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mundo] Terrain nativo exportado em " + CaminhoDoTerrain);
        }

        [MenuItem("Jogo do Grupo/Mundo/Montar prévia do Terrain nativo")]
        public static void MontarPrevia()
        {
            var dados = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoDoTerrain);
            if (dados == null)
            {
                Gerar();
                dados = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoDoTerrain);
            }

            var anterior = GameObject.Find(NomeDaPrevia);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }

            var previa = Terrain.CreateTerrainGameObject(dados);
            previa.name = NomeDaPrevia;
            previa.transform.position = new Vector3(
                -MapaDoMundo.Metade,
                MapaDoMundo.AlturaMinima,
                -MapaDoMundo.Metade
            );
            var componente = previa.GetComponent<Terrain>();
            componente.drawTreesAndFoliage = true;
            componente.detailObjectDensity = 1f;
            Selection.activeGameObject = previa;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        /// <summary>Confere que o asset preserva tamanho, faixa vertical e alturas do mapa atual.</summary>
        public static void Verificar()
        {
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoDoTerrain);
            if (terrain == null)
            {
                throw new InvalidOperationException("O TerrainData nativo ainda não foi gerado.");
            }

            if (
                terrain.heightmapResolution != ResolucaoDoHeightmap
                || terrain.detailPrototypes.Length != 0
                || terrain.terrainLayers.Length != 1
                || terrain.size
                    != new Vector3(
                        MapaDoMundo.Lado,
                        MapaDoMundo.AlturaMaxima - MapaDoMundo.AlturaMinima,
                        MapaDoMundo.Lado
                    )
            )
            {
                throw new InvalidOperationException(
                    "O TerrainData não tem as dimensões esperadas."
                );
            }

            foreach (var indice in new[] { 0, ResolucaoDoHeightmap / 2, ResolucaoDoHeightmap - 1 })
            {
                var coordenada = CoordenadaDoMundo(indice);
                var esperada =
                    MapaDoMundo.AlturaEm(coordenada, coordenada) - MapaDoMundo.AlturaMinima;
                var obtida = terrain.GetHeight(indice, indice);
                if (Mathf.Abs(esperada - obtida) > 0.01f)
                {
                    throw new InvalidOperationException(
                        "O TerrainData não preservou a altura do mapa."
                    );
                }
            }

            var previa = Terrain.CreateTerrainGameObject(terrain);
            try
            {
                var colisor = previa.GetComponent<TerrainCollider>();
                if (colisor == null || colisor.terrainData != terrain)
                {
                    throw new InvalidOperationException(
                        "A prévia não recebeu o colisor do Terrain."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(previa);
            }
        }

        [MenuItem("Jogo do Grupo/Mundo/Remover prévia do Terrain nativo")]
        public static void RemoverPrevia()
        {
            var previa = GameObject.Find(NomeDaPrevia);
            if (previa != null)
            {
                Object.DestroyImmediate(previa);
            }
        }

        internal static float[,] AlturasNormalizadas()
        {
            var alturas = new float[ResolucaoDoHeightmap, ResolucaoDoHeightmap];
            var faixa = MapaDoMundo.AlturaMaxima - MapaDoMundo.AlturaMinima;
            for (var z = 0; z < ResolucaoDoHeightmap; z++)
            {
                var mundoZ = CoordenadaDoMundo(z);
                for (var x = 0; x < ResolucaoDoHeightmap; x++)
                {
                    var altura = MapaDoMundo.AlturaEm(CoordenadaDoMundo(x), mundoZ);
                    alturas[z, x] = Mathf.Clamp01((altura - MapaDoMundo.AlturaMinima) / faixa);
                }
            }

            return alturas;
        }

        private static void PrepararCamadaDoSolo(TerrainData terrain)
        {
            TerrainLayer camada = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(CaminhoDoTerrain))
            {
                if (asset is TerrainLayer existente)
                {
                    camada = existente;
                    break;
                }
            }

            if (camada == null)
            {
                camada = new TerrainLayer();
                AssetDatabase.AddObjectToAsset(camada, terrain);
            }

            camada.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                CaminhoDaTexturaDaGrama
            );
            camada.tileSize = new Vector2(8f, 8f);
            terrain.terrainLayers = new[] { camada };
            EditorUtility.SetDirty(camada);
        }

        private static float CoordenadaDoMundo(int indice)
        {
            return -MapaDoMundo.Metade + MapaDoMundo.Lado * indice / (ResolucaoDoHeightmap - 1f);
        }
    }
}
