using UnityEditor;

namespace JogoDoGrupo.Client.Features.Itens.Editor
{
    /// <summary>
    /// Configura sozinho a importação dos ícones de equipamento (silhuetas brancas com alpha):
    /// Sprite, sem compressão, sem mipmaps, filtro bilinear. A UI tinge pela raridade.
    /// </summary>
    public sealed class ImportadorIconesEquipamento : AssetPostprocessor
    {
        public const string Pasta = "Assets/ThirdParty/GameIcons/Resources/Itens/Icones/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Pasta))
            {
                return;
            }

            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = false;
            importador.filterMode = UnityEngine.FilterMode.Bilinear;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = 128;
        }
    }
}
