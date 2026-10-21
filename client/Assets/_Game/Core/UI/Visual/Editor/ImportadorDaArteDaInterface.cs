using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Visual.Editor
{
    /// <summary>
    /// Configura sozinho a importação da arte da interface: as molduras geradas por
    /// <c>design/interface/ferramentas/gerar-texturas.mjs</c> e os ícones da interface e das
    /// skills (silhuetas brancas que a UI tinge). Sem mipmaps nem compressão, para as bordas
    /// fatiadas (9-slice) não borrarem. O fundo das telas de entrada (uma vista do mundo) vai
    /// comprimido e sem mipmaps: é grande e só aparece na resolução da tela.
    /// </summary>
    public sealed class ImportadorDaArteDaInterface : AssetPostprocessor
    {
        public const string PastaMolduras =
            "Assets/_Game/Core/UI/Visual/Resources/Lumora/Arte/Hud/";
        public const string PastaIcones = "Assets/ThirdParty/GameIcons/Resources/Interface/";
        public const string FundoDoMundo =
            "Assets/_Game/Core/UI/Visual/Resources/Lumora/Arte/fundo-mundo.jpg";

        private void OnPreprocessTexture()
        {
            if (assetPath == FundoDoMundo)
            {
                var fundo = (TextureImporter)assetImporter;
                fundo.textureType = TextureImporterType.Default;
                fundo.sRGBTexture = true;
                fundo.mipmapEnabled = false;
                fundo.wrapMode = TextureWrapMode.Clamp;
                fundo.filterMode = FilterMode.Bilinear;
                fundo.textureCompression = TextureImporterCompression.CompressedHQ;
                fundo.npotScale = TextureImporterNPOTScale.None;
                fundo.maxTextureSize = 2048;
                return;
            }

            var moldura = assetPath.StartsWith(PastaMolduras);
            var icone = assetPath.StartsWith(PastaIcones);
            if (!moldura && !icone)
                return;

            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Default;
            importador.sRGBTexture = true;
            importador.alphaSource = TextureImporterAlphaSource.FromInput;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = false;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.filterMode = FilterMode.Bilinear;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.maxTextureSize = icone ? 128 : 512;
        }
    }
}
