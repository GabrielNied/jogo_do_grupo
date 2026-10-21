namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// Números do áudio (docs/features/interface.md, Áudio): propostas a calibrar.
    /// </summary>
    public static class ConfigAudio
    {
        /// <summary>Duração da transição cruzada entre duas faixas.</summary>
        public const float TransicaoSegundos = 2f;

        /// <summary>Fração do volume da música com o personagem caído.</summary>
        public const float MusicaComPersonagemCaido = 0.5f;

        /// <summary>Volume relativo da música (as faixas CC0 vêm altas perto dos efeitos).</summary>
        public const float VolumeDaMusica = 0.55f;

        /// <summary>Volume relativo dos sons de interface.</summary>
        public const float VolumeDaInterface = 0.6f;

        /// <summary>Metros andados no chão entre dois passos do próprio personagem.</summary>
        public const float MetrosPorPasso = 2.1f;

        /// <summary>Volume relativo dos passos do próprio personagem.</summary>
        public const float VolumeDosPassos = 0.35f;

        /// <summary>Intervalo mínimo entre dois cliques de interface (evita estalo duplo).</summary>
        public const float IntervaloMinimoDoCliqueSegundos = 0.05f;
    }
}
