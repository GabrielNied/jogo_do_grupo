namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Client: notifica o desafiado que um jogador quer duelar.
    /// </summary>
    [System.Serializable]
    public class ConviteDeDueloRecebido
    {
        public long DesafianteId;
        public string NomeDesafiante = "";
        public int SegundosParaExpirar = RegrasPvp.TempoExpiracaoConviteDueloSegundos;
    }
}
