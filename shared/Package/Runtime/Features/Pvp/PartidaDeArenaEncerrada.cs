namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Participantes: partida finalizada com equipe vencedora e alteração de rating.
    /// </summary>
    [System.Serializable]
    public class PartidaDeArenaEncerrada
    {
        public string IdPartida = "";
        public int EquipeVencedora; // 0 = Azul, 1 = Vermelho, -1 = Empate
        public int PontosVariacao;
        public int NovoRanking;
    }
}
