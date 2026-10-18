namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Client: resposta com dados de ranking e estatísticas de arena do jogador.
    /// </summary>
    [System.Serializable]
    public class RespostaRankingArena
    {
        public int Pontos = RegrasPvp.PontosIniciaisArena;
        public int Vitorias;
        public int Derrotas;
        public int Temporada = 1;
    }
}
