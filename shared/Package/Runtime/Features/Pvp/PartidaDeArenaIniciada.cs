namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Participantes: partida de arena 3v3 iniciada com equipes e contagem de preparo.
    /// </summary>
    [System.Serializable]
    public class PartidaDeArenaIniciada
    {
        public string IdPartida = "";
        public string Area = "";
        public long[] TimeAzul = System.Array.Empty<long>();
        public long[] TimeVermelho = System.Array.Empty<long>();
        public int TempoPreparoSegundos = RegrasPvp.TempoPreparoArenaSegundos;
    }
}
