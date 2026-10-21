namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Servidor → client (12008): informa as etapas da raid já concluídas pelo personagem na semana atual.
    /// </summary>
    [System.Serializable]
    public class RespostaBloqueioSemanalRaid
    {
        public string ChaveSemana = string.Empty;
        public int[] EtapasConcluidas = new int[0];
    }
}
