namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>Regra pura de qual faixa toca agora (testável sem Unity tocar som).</summary>
    public static class EscolhaDaMusica
    {
        /// <summary>
        /// Fora do mundo, a abertura; dentro, a faixa pedida pelo mundo (cidade se ninguém pediu
        /// outra).
        /// </summary>
        public static MusicaDoJogo Escolher(bool noMundo, MusicaDoJogo pedidaPeloMundo)
        {
            if (!noMundo)
            {
                return MusicaDoJogo.Abertura;
            }

            return
                pedidaPeloMundo == MusicaDoJogo.Nenhuma || pedidaPeloMundo == MusicaDoJogo.Abertura
                ? MusicaDoJogo.Cidade
                : pedidaPeloMundo;
        }

        /// <summary>Faixa que segue quando a <paramref name="atual"/> acaba sozinha.</summary>
        public static MusicaDoJogo DepoisDe(MusicaDoJogo atual) =>
            atual == MusicaDoJogo.Vitoria ? MusicaDoJogo.Dungeon : atual;

        /// <summary>A faixa repete? Só a vitória toca uma vez.</summary>
        public static bool Repete(MusicaDoJogo musica) => musica != MusicaDoJogo.Vitoria;
    }
}
