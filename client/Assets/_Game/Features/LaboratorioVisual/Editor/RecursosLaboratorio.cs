using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.LaboratorioVisual.Editor
{
    public sealed class RecursosLaboratorio
    {
        public const string Pasta = "Assets/_Game/Features/LaboratorioVisual/Editor/";
        public readonly Font Leitura;
        public readonly Font Controle;
        public readonly Font Titulo;
        public readonly Texture2D Logo;
        public readonly Texture2D Moldura;
        public readonly Texture2D Fundo;
        public readonly Texture2D Pergaminho;
        public readonly Texture2D[] Pranchas;
        public readonly StyleSheet Estilo;
        public readonly string Impressao;

        public RecursosLaboratorio()
        {
            var raiz = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var hash = new StringBuilder();
            T Importar<T>(string fonte, string destino)
                where T : UnityEngine.Object
            {
                var origem = Path.Combine(raiz, "design", fonte);
                var asset = Pasta + "RecursosGerados/" + destino;
                var absoluto = Path.Combine(Application.dataPath, "..", asset);
                if (!File.Exists(origem))
                    throw new FileNotFoundException(
                        "Recurso de design ausente. Confira o Git LFS.",
                        origem
                    );
                var bytes = File.ReadAllBytes(origem);
                using var sha = SHA256.Create();
                var digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                hash.AppendLine(fonte + " SHA256 " + digest);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluto));
                if (!File.Exists(absoluto) || !Iguais(bytes, File.ReadAllBytes(absoluto)))
                    File.WriteAllBytes(absoluto, bytes);
                AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
                if (typeof(T) == typeof(Texture2D))
                {
                    var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                    if (
                        importer != null
                        && (
                            importer.mipmapEnabled
                            || !importer.alphaIsTransparency
                            || importer.textureCompression
                                != TextureImporterCompression.Uncompressed
                        )
                    )
                    {
                        importer.mipmapEnabled = false;
                        importer.alphaIsTransparency = true;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                    }
                }
                return AssetDatabase.LoadAssetAtPath<T>(asset)
                    ?? throw new InvalidOperationException("Unity não importou " + asset);
            }

            Leitura = Importar<Font>(
                "laboratorio-unity/fontes/LumoraSource-500.ttf",
                "leitura.ttf"
            );
            Controle = Importar<Font>(
                "laboratorio-unity/fontes/LumoraSource-700.ttf",
                "controle.ttf"
            );
            Titulo = Importar<Font>("laboratorio-unity/fontes/LumoraNunito-800.ttf", "titulo.ttf");
            Logo = Importar<Texture2D>("marca/logo-horizontal.png", "logo.png");
            Moldura = Importar<Texture2D>(
                "interface/elementos/moldura-folhas-calcada.png",
                "moldura.png"
            );
            Fundo = Importar<Texture2D>("interface/elementos/fundo-petrol.png", "fundo.png");
            Pergaminho = Importar<Texture2D>(
                "interface/elementos/pergaminho.png",
                "pergaminho.png"
            );
            var nomes = new[]
            {
                "01-componentes",
                "03-grupo-social",
                "02-hud-inventario",
                "02-hud-inventario",
                "04-cidade-economia",
            };
            Pranchas = new Texture2D[nomes.Length];
            for (var i = 0; i < nomes.Length; i++)
                Pranchas[i] = Importar<Texture2D>(
                    "interface/pranchas/" + nomes[i] + ".png",
                    nomes[i] + ".png"
                );
            Estilo =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(Pasta + "Laboratorio.uss")
                ?? throw new InvalidOperationException("Estilo do laboratório ausente.");
            Impressao = hash.ToString();
        }

        private static bool Iguais(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}
