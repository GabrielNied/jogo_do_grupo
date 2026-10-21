using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Visual.Editor
{
    /// <summary>
    /// Configura sozinho a importação da arte da interface: as molduras geradas por
    /// <c>design/interface/ferramentas/gerar-texturas.mjs</c> e os ícones da interface e das
    /// skills (silhuetas brancas que a UI tinge). Sem mipmaps nem compressão, para as bordas
    /// fatiadas (9-slice) não borrarem.
    /// </summary>
    public sealed class ImportadorDaArteDaInterface : AssetPostprocessor
    {
        public const string PastaMolduras =
            "Assets/_Game/Core/UI/Visual/Resources/Lumora/Arte/Hud/";
        public const string PastaIcones = "Assets/ThirdParty/GameIcons/Resources/Interface/";

        private void OnPreprocessTexture()
        {
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
