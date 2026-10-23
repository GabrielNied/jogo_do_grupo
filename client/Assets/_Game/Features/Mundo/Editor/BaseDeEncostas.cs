using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Somente pesos originais compactados, sem duplicar heightmap, arvores ou detalhes.</summary>
    public sealed class BaseDeEncostas : ScriptableObject
    {
        public TerrainData Terreno;
        public TerrainLayer[] Camadas;
        public int Resolucao;
        public byte[] Pesos;
        public string HashEsperado;
        public string HashBase;

        public void Capturar(TerrainData dados)
        {
            Terreno = dados;
            Camadas = dados.terrainLayers;
            Resolucao = dados.alphamapResolution;
            using var saida = new MemoryStream();
            using (
                var zip = new DeflateStream(
                    saida,
                    System.IO.Compression.CompressionLevel.Optimal,
                    true
                )
            )
            using (var writer = new BinaryWriter(zip))
            {
                // Faixas evitam outra copia integral em floats enquanto capturamos a base.
                for (var z = 0; z < Resolucao; z += 32)
                {
                    var faixa = dados.GetAlphamaps(0, z, Resolucao, Math.Min(32, Resolucao - z));
                    foreach (var peso in faixa)
                        writer.Write(peso);
                }
            }
            Pesos = saida.ToArray();
            HashBase = HashEsperado = Assinatura(dados);
        }

        public bool Confere(TerrainData dados) =>
            dados == Terreno
            && Resolucao == dados.alphamapResolution
            && Assinatura(dados) == HashEsperado;

        public void Restaurar(
            TerrainData dados,
            ConfigDeEncostas regiao = null,
            Vector3 origem = default
        )
        {
            if (dados != Terreno || Resolucao != dados.alphamapResolution)
                throw new InvalidOperationException(
                    "A resolucao ou o TerrainData mudou; a base nao e compativel."
                );
            var regional = regiao != null && regiao.LimitarRegiao;
            if (!regional)
                dados.terrainLayers = Camadas;
            var atuais = dados.terrainLayers;
            var indices = Array.ConvertAll(Camadas, c => Array.IndexOf(atuais, c));
            if (Array.Exists(indices, i => i < 0))
                throw new InvalidOperationException(
                    "A camada original nao existe mais no Terrain; restaure a base inteira."
                );
            using var entrada = new MemoryStream(Pesos);
            using var zip = new DeflateStream(entrada, CompressionMode.Decompress);
            using var reader = new BinaryReader(zip);
            for (var z = 0; z < Resolucao; z += 32)
            {
                var linhas = Math.Min(32, Resolucao - z);
                var faixa = regional
                    ? dados.GetAlphamaps(0, z, Resolucao, linhas)
                    : new float[linhas, Resolucao, Camadas.Length];
                for (var linha = 0; linha < faixa.GetLength(0); linha++)
                for (var x = 0; x < Resolucao; x++)
                {
                    var dentro =
                        !regional
                        || regiao.PesoRegiao(
                            origem.x + (x + 0.5f) / Resolucao * dados.size.x,
                            origem.z + (z + linha + 0.5f) / Resolucao * dados.size.z
                        ) > 0f;
                    if (dentro)
                        for (var camada = 0; camada < atuais.Length; camada++)
                            faixa[linha, x, camada] = 0f;
                    for (var camada = 0; camada < Camadas.Length; camada++)
                    {
                        var peso = reader.ReadSingle();
                        if (dentro)
                            faixa[linha, x, indices[camada]] = peso;
                    }
                }
                dados.SetAlphamaps(0, z, faixa);
            }
            dados.SetBaseMapDirty();
        }

        public static string Assinatura(TerrainData dados)
        {
            using var hash = SHA256.Create();
            void Acrescentar(byte[] bytes) => hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            foreach (var camada in dados.terrainLayers)
            {
                var caminho = AssetDatabase.GetAssetPath(camada);
                var id = string.IsNullOrEmpty(caminho)
                    ? camada.GetEntityId().ToString()
                    : AssetDatabase.AssetPathToGUID(caminho);
                Acrescentar(System.Text.Encoding.UTF8.GetBytes(id + ";"));
            }
            // Alphamaps sao quantizadas em 8 bits pelo Terrain. Hash por byte evita falso
            // conflito de floats apos salvar/reabrir, e detecta retoque ate fora da mascara.
            var largura = dados.alphamapWidth;
            for (var z = 0; z < dados.alphamapHeight; z += 32)
            {
                var faixa = dados.GetAlphamaps(
                    0,
                    z,
                    largura,
                    Math.Min(32, dados.alphamapHeight - z)
                );
                var bytes = new byte[faixa.Length];
                var indice = 0;
                foreach (var peso in faixa)
                    bytes[indice++] = (byte)Mathf.RoundToInt(Mathf.Clamp01(peso) * 255f);
                Acrescentar(bytes);
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToBase64String(hash.Hash);
        }

        public static BaseDeEncostas Obter(TerrainData dados, bool criar)
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dados));
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("Salve o TerrainData antes de aplicar.");
            const string pasta = "Assets/_Game/Features/Mundo/Editor/BasesDeEncostas";
            var caminho = pasta + "/" + guid + ".asset";
            var existente = AssetDatabase.LoadAssetAtPath<BaseDeEncostas>(caminho);
            if (existente != null || !criar)
                return existente;
            if (AssetDatabase.LoadMainAssetAtPath(caminho) != null)
                throw new InvalidOperationException(
                    "O caminho reservado para o backup ja contem outro asset: " + caminho
                );
            if (!AssetDatabase.IsValidFolder(pasta))
                AssetDatabase.CreateFolder("Assets/_Game/Features/Mundo/Editor", "BasesDeEncostas");
            var nova = CreateInstance<BaseDeEncostas>();
            nova.Capturar(dados);
            AssetDatabase.CreateAsset(nova, caminho);
            return nova;
        }
    }
}
