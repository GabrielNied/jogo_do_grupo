using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Visual.Editor
{
    /// <summary>
    /// Configura sozinho a importação do áudio do jogo (fica no assembly de editor do Core). As
    /// músicas tocam em streaming (não ocupam a memória inteira descomprimidas); os efeitos curtos
    /// descomprimem ao carregar, para tocar sem atraso.
    /// </summary>
    public sealed class ImportadorDoAudio : AssetPostprocessor
    {
        public const string PastaDasMusicas = "Assets/ThirdParty/OpenGameArt/Musica/";
        public const string PastaDosEfeitos = "Assets/ThirdParty/Kenney/Audio/";

        private void OnPreprocessAudio()
        {
            var musica = assetPath.StartsWith(PastaDasMusicas);
            var efeito = assetPath.StartsWith(PastaDosEfeitos);
            if (!musica && !efeito)
                return;

            var importador = (AudioImporter)assetImporter;
            var ajuste = importador.defaultSampleSettings;
            ajuste.compressionFormat = AudioCompressionFormat.Vorbis;
            if (musica)
            {
                ajuste.loadType = AudioClipLoadType.Streaming;
                ajuste.quality = 0.6f;
                importador.forceToMono = false;
                importador.loadInBackground = true;
            }
            else
            {
                ajuste.loadType = AudioClipLoadType.DecompressOnLoad;
                ajuste.quality = 0.7f;
                importador.forceToMono = true;
            }

            importador.defaultSampleSettings = ajuste;
        }
    }
}
