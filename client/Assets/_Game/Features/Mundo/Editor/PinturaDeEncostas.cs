using System;
using System.Collections.Generic;
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

        public static bool Buraco(TerrainData dados, float u, float v) =>
            dados.IsHole(
                Mathf.Min((int)(u * dados.holesResolution), dados.holesResolution - 1),
                Mathf.Min((int)(v * dados.holesResolution), dados.holesResolution - 1)
            );

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

            Aplicar(
                dados,
                Vector3.zero,
                new ConfigDeEncostas
                {
                    Camada = rocha,
                    Inicio = inicio,
                    Plena = coberturaPlena,
                    Cobertura = intensidade,
                },
                cancelar
            );
        }

        public static void Aplicar(
            TerrainData dados,
            Vector3 origem,
            ConfigDeEncostas receita,
            Func<float, bool> cancelar = null
        )
        {
            var erro = receita.Validar();
            if (erro != null)
                throw new ArgumentException(erro);
            var rocha = receita.Camada;
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
                    var mundo =
                        origem
                        + new Vector3(
                            u * dados.size.x,
                            dados.GetInterpolatedHeight(u, v),
                            v * dados.size.z
                        );
                    var alvo = receita.Mascara(mundo, dados.GetSteepness(u, v));
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

        public static void RegistrarUndo(TerrainData dados)
        {
            Undo.RegisterCompleteObjectUndo(dados, "Pintar encostas");
            foreach (var textura in dados.alphamapTextures)
                Undo.RegisterCompleteObjectUndo(textura, "Pintar encostas");
        }

        public static void AplicarLote(
            Terrain[] terrenos,
            ConfigDeEncostas receita,
            bool restaurar,
            Func<float, bool> cancelar = null
        )
        {
            // Confere TODOS antes de escrever no primeiro. Nunca incorpora retoques implicitamente.
            var erro = receita.Validar();
            if (erro != null)
                throw new ArgumentException(erro);
            long bytesDeBase = 0;
            foreach (var terreno in terrenos)
            {
                var dados = terreno.terrainData;
                bytesDeBase +=
                    (long)dados.alphamapWidth
                    * dados.alphamapHeight
                    * dados.alphamapLayers
                    * sizeof(float);
                if (bytesDeBase > 512L * 1024 * 1024)
                    throw new InvalidOperationException(
                        "Lote excede 512 MiB de pesos originais. Selecione menos terrenos para limitar backup e Undo."
                    );
                var registro = BaseDeEncostas.Obter(terreno.terrainData, false);
                if (registro != null && !registro.Confere(terreno.terrainData))
                    throw new InvalidOperationException(
                        terreno.name
                            + ": pintura/camadas alteradas desde a ultima aplicacao. "
                            + "Use Assumir pintura atual como nova base para preservar os retoques."
                    );
            }
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(restaurar ? "Restaurar base das encostas" : "Pintar encostas");
            var basesCriadas = new List<BaseDeEncostas>();
            try
            {
                for (var i = 0; i < terrenos.Length; i++)
                {
                    if (cancelar?.Invoke((float)i / terrenos.Length) == true)
                        throw new OperationCanceledException();
                    var terreno = terrenos[i];
                    var dados = terreno.terrainData;
                    var registro = BaseDeEncostas.Obter(dados, false);
                    if (registro == null && !restaurar)
                    {
                        registro = BaseDeEncostas.Obter(dados, true);
                        basesCriadas.Add(registro);
                    }
                    if (registro == null)
                        continue;
                    RegistrarUndo(dados);
                    Undo.RegisterCompleteObjectUndo(registro, "Atualizar base das encostas");
                    registro.Restaurar(dados, receita, terreno.transform.position);
                    if (!restaurar)
                    {
                        var progressoInicial = (float)i / terrenos.Length;
                        Aplicar(
                            dados,
                            terreno.transform.position,
                            receita,
                            p => cancelar?.Invoke(progressoInicial + p / terrenos.Length) == true
                        );
                    }
                    registro.HashEsperado = BaseDeEncostas.Assinatura(dados);
                    EditorUtility.SetDirty(registro);
                    EditorUtility.SetDirty(dados);
                    terreno.Flush();
                }
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                foreach (var registro in basesCriadas)
                {
                    Undo.ClearUndo(registro);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(registro));
                }
                throw;
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }
    }
}
