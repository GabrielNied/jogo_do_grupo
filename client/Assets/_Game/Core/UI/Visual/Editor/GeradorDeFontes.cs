using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace JogoDoGrupo.Client.Core.Visual.Editor
{
    /// <summary>
    /// Gera o Font Asset SDF de cada papel de <see cref="ConfigVisual.Fontes"/> a partir do
    /// <c>.ttf</c> de mesmo nome (sem o sufixo <c>-SDF</c>), ao lado dele em
    /// <c>Resources/Lumora/Fontes</c> (ADR 0035). Rodar de novo recria os assets.
    /// Linha de comando:
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Core.Visual.Editor.GeradorDeFontes.Gerar</c>.
    /// </summary>
    public static class GeradorDeFontes
    {
        public const string PastaResources = "Assets/_Game/Core/UI/Visual/Resources/";
        public const string SufixoSdf = "-SDF";

        // Valores ajustáveis. Texto de 12 a 32 px, até 4/3 disso em 2560×1440: amostrar em 56
        // com margem de ~10% mantém o contorno nítido sem estourar o atlas.
        const int TamanhoAmostragem = 56;
        const int Margem = 6;
        const int LadoAtlas = 1024;
        const GlyphRenderMode Renderizacao = GlyphRenderMode.SDFAA;

        // Símbolos da interface fora do ASCII e do Latin-1.
        const string Simbolos = "▲▼▶◀→←↑↓•·×−–—“”‘’…";

        /// <summary>ASCII, Latin-1 (acentos do português) e os símbolos usados nas telas.</summary>
        public static string Caracteres()
        {
            var texto = new StringBuilder();
            for (var c = 0x20; c <= 0x7E; c++)
                texto.Append((char)c);
            for (var c = 0xA0; c <= 0xFF; c++)
                texto.Append((char)c);
            texto.Append(Simbolos);
            return texto.ToString();
        }

        [MenuItem("Jogo do Grupo/Visual/Gerar fontes SDF")]
        public static void Gerar()
        {
            var ausentes = new List<string>();
            foreach (var recurso in Recursos())
            {
                var falta = GerarFonte(recurso);
                if (!string.IsNullOrEmpty(falta))
                    ausentes.Add(recurso + ": " + falta);
            }
            // A fonte de leitura cobre os símbolos que as outras não têm (ex.: ▲▼ na Nunito).
            var leitura = Carregar(ConfigVisual.Fontes.Leitura);
            foreach (var recurso in Recursos())
            {
                var asset = Carregar(recurso);
                if (asset == leitura)
                    continue;
                asset.fallbackFontAssetTable = new List<FontAsset> { leitura };
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            // O USS guarda a referência do asset ao importar: reimportar depois de recriar.
            AssetDatabase.ImportAsset(
                PastaResources + "Lumora/TokensVisuais.uss",
                ImportAssetOptions.ForceUpdate
            );
            if (ausentes.Count > 0)
                Debug.LogWarning(
                    "[GeradorDeFontes] Glifos ausentes: " + string.Join("; ", ausentes)
                );
            Debug.Log("[GeradorDeFontes] Fontes SDF geradas.");
        }

        static FontAsset Carregar(string recurso)
        {
            return AssetDatabase.LoadAssetAtPath<FontAsset>(PastaResources + recurso + ".asset");
        }

        static IEnumerable<string> Recursos()
        {
            var vistos = new HashSet<string>();
            foreach (
                var recurso in new[]
                {
                    ConfigVisual.Fontes.Leitura,
                    ConfigVisual.Fontes.Controle,
                    ConfigVisual.Fontes.Titulo,
                }
            )
            {
                if (vistos.Add(recurso))
                    yield return recurso;
            }
        }

        /// <summary>Cria o asset e devolve os caracteres que a fonte não tem.</summary>
        static string GerarFonte(string recurso)
        {
            if (!recurso.EndsWith(SufixoSdf, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Fonte sem sufixo " + SufixoSdf + ": " + recurso
                );
            var caminhoAsset = PastaResources + recurso + ".asset";
            var caminhoTtf =
                PastaResources + recurso.Substring(0, recurso.Length - SufixoSdf.Length) + ".ttf";
            var fonte =
                AssetDatabase.LoadAssetAtPath<Font>(caminhoTtf)
                ?? throw new InvalidOperationException("Fonte não importada: " + caminhoTtf);

            var asset =
                FontAsset.CreateFontAsset(
                    fonte,
                    TamanhoAmostragem,
                    Margem,
                    Renderizacao,
                    LadoAtlas,
                    LadoAtlas,
                    AtlasPopulationMode.Dynamic,
                    true
                ) ?? throw new InvalidOperationException("Font Asset não criado: " + caminhoTtf);
            asset.name = Path.GetFileNameWithoutExtension(caminhoAsset);
            asset.TryAddCharacters(Caracteres(), out var ausentes);
            // Dinâmico (o estático está obsoleto no Unity 6.6), mas já com os glifos do jogo:
            // só um caractere fora da lista cresce o atlas (e vira diff no asset).

            AssetDatabase.DeleteAsset(caminhoAsset);
            AssetDatabase.CreateAsset(asset, caminhoAsset);
            for (var i = 0; i < asset.atlasTextures.Length; i++)
            {
                var textura = asset.atlasTextures[i];
                if (textura == null)
                    continue;
                textura.name = asset.name + " Atlas" + (i == 0 ? "" : " " + i);
                AssetDatabase.AddObjectToAsset(textura, asset);
            }
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            return ausentes;
        }
    }
}
