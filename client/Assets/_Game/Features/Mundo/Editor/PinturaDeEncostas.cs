using System;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Pintura aditiva e idempotente sobre as camadas nativas do Terrain.</summary>
    public static class PinturaDeEncostas
    {
        public const float InicioPadrao = 35f;
        public const float CoberturaPlenaPadrao = 55f;
        public const float IntensidadePadrao = 1f;
        private const int AlturaDaFaixa = 32;

        public static void AplicarComUndo(
            TerrainData dados,
            TerrainLayer rocha,
            float inicio,
            float coberturaPlena,
            float intensidade,
            Func<float, bool> cancelar = null
        )
        {
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Pintar rocha nas encostas");
            try
            {
                // Alphamaps sao subassets: TerrainData sozinho nao restaura os pixels.
                Undo.RegisterCompleteObjectUndo(dados, "Pintar rocha nas encostas");
                foreach (var textura in dados.alphamapTextures)
                    Undo.RegisterCompleteObjectUndo(textura, "Pintar rocha nas encostas");
                Aplicar(dados, rocha, inicio, coberturaPlena, intensidade, cancelar);
                EditorUtility.SetDirty(dados);
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        public static void Aplicar(
            TerrainData dados,
            TerrainLayer rocha,
            float inicio,
            float coberturaPlena,
            float intensidade,
            Func<float, bool> cancelar = null
        )
        {
            if (dados == null || rocha == null || dados.terrainLayers.Length == 0)
                throw new ArgumentException(
                    "Escolha um terreno com solo pintado e uma camada de rocha."
                );
            if (
                !float.IsFinite(inicio)
                || !float.IsFinite(coberturaPlena)
                || !float.IsFinite(intensidade)
                || inicio < 0f
                || coberturaPlena > 90f
                || coberturaPlena <= inicio
                || intensidade <= 0f
                || intensidade > 1f
            )
                throw new ArgumentException(
                    "Use angulos crescentes entre 0 e 90 e intensidade entre 0 e 1."
                );

            var camadas = dados.terrainLayers;
            var indice = Array.IndexOf(camadas, rocha);
            if (indice < 0)
            {
                indice = camadas.Length;
                Array.Resize(ref camadas, camadas.Length + 1);
                camadas[indice] = rocha;
                dados.terrainLayers = camadas;
            }

            var largura = dados.alphamapWidth;
            var altura = dados.alphamapHeight;
            for (var linha = 0; linha < altura; linha += AlturaDaFaixa)
            {
                if (cancelar?.Invoke((float)linha / altura) == true)
                    throw new OperationCanceledException();
                var linhas = Math.Min(AlturaDaFaixa, altura - linha);
                var pesos = dados.GetAlphamaps(0, linha, largura, linhas);
                var mudou = false;
                for (var z = 0; z < linhas; z++)
                for (var x = 0; x < largura; x++)
                {
                    // Amostra no centro do texel, em coordenadas normalizadas do Terrain.
                    var u = (x + 0.5f) / largura;
                    var v = (linha + z + 0.5f) / altura;
                    if (
                        dados.IsHole(
                            Mathf.Min((int)(u * dados.holesResolution), dados.holesResolution - 1),
                            Mathf.Min((int)(v * dados.holesResolution), dados.holesResolution - 1)
                        )
                    )
                        continue;
                    var t = Mathf.InverseLerp(inicio, coberturaPlena, dados.GetSteepness(u, v));
                    var alvo = Mathf.SmoothStep(0f, intensidade, t);
                    var atual = pesos[z, x, indice];
                    if (alvo <= atual)
                        continue;
                    var fator = (1f - alvo) / (1f - atual);
                    for (var camada = 0; camada < camadas.Length; camada++)
                        pesos[z, x, camada] *= fator;
                    pesos[z, x, indice] = alvo;
                    mudou = true;
                }
                if (mudou)
                    dados.SetAlphamaps(0, linha, pesos);
            }
            dados.SetBaseMapDirty();
        }
    }
}
