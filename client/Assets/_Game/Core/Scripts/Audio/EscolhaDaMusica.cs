namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>Regra pura de qual faixa toca agora (testável sem Unity tocar som).</summary>
    public static class EscolhaDaMusica
    {
        /// <summary>
        /// Fora do mundo, a abertura; dentro, a faixa pedida pelo mundo (cidade se ninguém pediu
        /// outra).
        /// </summary>
        public static MusicaDoJogo Escolher(bool noMundo, MusicaDoJogo pedidaPeloMundo) =>
            ResolverFaixa(
                noMundo,
                pedidaPeloMundo,
                pedidaPeloMundo == MusicaDoJogo.Chefe,
                pedidaPeloMundo == MusicaDoJogo.Vitoria
            );

        /// <summary>
        /// Resolve a faixa considerando se o jogador está no mundo, a área base pedida pelo mundo,
        /// se há luta de chefe ativa e se o jingle de vitória está tocando.
        /// </summary>
        public static MusicaDoJogo ResolverFaixa(
            bool noMundo,
            MusicaDoJogo areaBase,
            bool bossEmLuta,
            bool vitoriaAtiva = false
        )
        {
            if (!noMundo)
            {
                return MusicaDoJogo.Abertura;
            }

            if (vitoriaAtiva || areaBase == MusicaDoJogo.Vitoria)
            {
                return MusicaDoJogo.Vitoria;
            }

            if (bossEmLuta || areaBase == MusicaDoJogo.Chefe)
            {
                return MusicaDoJogo.Chefe;
            }

            return areaBase == MusicaDoJogo.Nenhuma || areaBase == MusicaDoJogo.Abertura
                ? MusicaDoJogo.Cidade
                : areaBase;
        }

        /// <summary>Faixa que segue quando a <paramref name="atual"/> acaba sozinha.</summary>
        public static MusicaDoJogo DepoisDe(MusicaDoJogo atual) =>
            atual == MusicaDoJogo.Vitoria ? MusicaDoJogo.Dungeon : atual;

        /// <summary>A faixa repete? Só a vitória toca uma vez.</summary>
        public static bool Repete(MusicaDoJogo musica) => musica != MusicaDoJogo.Vitoria;
    }
}
