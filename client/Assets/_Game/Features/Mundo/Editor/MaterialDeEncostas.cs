using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Material opcional de autoria, sem alterar TerrainLayers ou materiais existentes.</summary>
    public static class MaterialDeEncostas
    {
        public const string NomeDoShader = "Jogo do Grupo/Mundo/Terrain com encostas triplanares";
        private const string NomeDoUndo = "Aplicar material triplanar nas encostas";
        private const int CamadasNoPassePrincipal = 4;

        public static bool Validar(Terrain[] terrenos, TerrainLayer camada, out string erro)
        {
            erro = null;
            if (terrenos == null || terrenos.Length == 0 || camada == null)
                erro = "Escolha terrenos e uma TerrainLayer para a projecao triplanar.";
            else if (camada.diffuseTexture == null || !EscalaValida(camada))
                erro = "A camada precisa de textura de cor e tamanho de repeticao positivo.";
            else if (
                GraphicsSettings.currentRenderPipeline == null
                || GraphicsSettings.currentRenderPipeline.GetType().Name
                    != "UniversalRenderPipelineAsset"
            )
                erro = "O material de encostas requer Universal Render Pipeline (URP).";
            if (erro != null)
                return false;
            foreach (var terreno in terrenos)
            {
                if (terreno == null || terreno.terrainData == null)
                {
                    erro = "Todos os terrenos precisam de TerrainData.";
                    return false;
                }
                var indice = Array.IndexOf(terreno.terrainData.terrainLayers, camada);
                if (indice < 0 || indice >= CamadasNoPassePrincipal)
                {
                    erro =
                        $"A camada escolhida precisa estar entre as primeiras quatro camadas de {terreno.name}. "
                        + "Escolha uma dessas camadas ou mantenha o material nativo. "
                        + "As camadas seguintes continuam no passe adicional nativo, sem triplanar nelas.";
                    return false;
                }
            }
            var shader = Shader.Find(NomeDoShader);
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                erro = "O shader de encostas nao esta disponivel ou apresentou erro de compilacao.";
            return erro == null;
        }

        public static Material Criar(string caminho, TerrainLayer camada)
        {
            if (
                string.IsNullOrWhiteSpace(caminho)
                || !caminho.StartsWith("Assets/", StringComparison.Ordinal)
                || !caminho.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)
                || !AssetDatabase.IsValidFolder(Path.GetDirectoryName(caminho)?.Replace('\\', '/'))
                || AssetDatabase.LoadMainAssetAtPath(caminho) != null
                || File.Exists(caminho)
            )
                throw new ArgumentException(
                    "Escolha um caminho novo de material dentro de Assets."
                );
            if (camada == null || camada.diffuseTexture == null || !EscalaValida(camada))
                throw new ArgumentException(
                    "Escolha uma TerrainLayer com textura e escala positiva."
                );
            var shader = Shader.Find(NomeDoShader);
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException(
                    "O shader triplanar nao compilou neste projeto."
                );
            var material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(caminho),
            };
            material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
            material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
            ConfigurarEscala(material, camada);
            AssetDatabase.CreateAsset(material, caminho);
            return material;
        }

        public static Material CriarEAplicar(
            string caminho,
            Terrain[] terrenos,
            TerrainLayer camada
        )
        {
            if (!Validar(terrenos, camada, out var erro))
                throw new ArgumentException(erro);
            var material = Criar(caminho, camada);
            try
            {
                Aplicar(terrenos, material, camada);
                return material;
            }
            catch
            {
                // Criar rejeita assets existentes: este caminho pertence somente
                // a esta tentativa, e Aplicar ja restaurou todos os terrenos.
                AssetDatabase.DeleteAsset(caminho);
                throw;
            }
        }

        public static void Aplicar(Terrain[] terrenos, Material material, TerrainLayer camada)
        {
            if (!Validar(terrenos, camada, out var erro))
                throw new ArgumentException(erro);
            if (
                material == null
                || material.shader.name != NomeDoShader
                || !AssetDatabase.Contains(material)
            )
                throw new ArgumentException(
                    "Crie primeiro um novo material de encostas salvo no projeto."
                );
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(NomeDoUndo);
            var criados = new List<Material>();
            try
            {
                foreach (var terreno in terrenos)
                {
                    // Um submaterial por terreno mantem o indice local sem alterar a ordem das camadas.
                    var instancia = new Material(material)
                    {
                        name = $"{material.name} - {terreno.name}",
                    };
                    criados.Add(instancia);
                    PreservarMistura(terreno.materialTemplate, instancia);
                    var indice = Array.IndexOf(terreno.terrainData.terrainLayers, camada);
                    var selecao = Vector4.zero;
                    selecao[indice] = 1f;
                    instancia.SetVector("_EncostasCamadas", selecao);
                    instancia.SetFloat("_EncostasDistanciaBase", terreno.basemapDistance);
                    ConfigurarEscala(instancia, camada);
                    AssetDatabase.AddObjectToAsset(instancia, material);
                    Undo.RegisterCreatedObjectUndo(instancia, NomeDoUndo);
                    Undo.RegisterCompleteObjectUndo(terreno, NomeDoUndo);
                    terreno.materialTemplate = instancia;
                    terreno.terrainData.SetBaseMapDirty();
                    terreno.Flush();
                    EditorUtility.SetDirty(terreno);
                }
                EditorUtility.SetDirty(material);
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                // AddObjectToAsset pode falhar antes de a criacao entrar no Undo.
                // Apenas os objetos desta tentativa podem ser removidos aqui.
                foreach (var instancia in criados)
                    if (instancia != null)
                        UnityEngine.Object.DestroyImmediate(instancia, true);
                throw;
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        private static void ConfigurarEscala(Material material, TerrainLayer camada)
        {
            material.SetVector(
                "_EncostasEscala",
                new Vector4(
                    1f / Mathf.Max(0.001f, camada.tileSize.x),
                    1f / Mathf.Max(0.001f, camada.tileSize.y),
                    camada.tileOffset.x / Mathf.Max(0.001f, camada.tileSize.x),
                    camada.tileOffset.y / Mathf.Max(0.001f, camada.tileSize.y)
                )
            );
        }

        private static bool EscalaValida(TerrainLayer camada) =>
            float.IsFinite(camada.tileSize.x)
            && float.IsFinite(camada.tileSize.y)
            && float.IsFinite(camada.tileOffset.x)
            && float.IsFinite(camada.tileOffset.y)
            && camada.tileSize.x > 0f
            && camada.tileSize.y > 0f;

        private static void PreservarMistura(Material anterior, Material novo)
        {
            if (anterior == null)
                return;
            foreach (var propriedade in new[] { "_EnableHeightBlend", "_HeightTransition" })
                if (anterior.HasProperty(propriedade))
                    novo.SetFloat(propriedade, anterior.GetFloat(propriedade));
            if (novo.GetFloat("_EnableHeightBlend") > 0f)
                novo.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
            else
                novo.DisableKeyword("_TERRAIN_BLEND_HEIGHT");
        }
    }
}
