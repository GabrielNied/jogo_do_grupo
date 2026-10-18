namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Participantes e observadores: duelo 1v1 iniciado com contagem regressiva.
    /// </summary>
    [System.Serializable]
    public class DueloIniciado
    {
        public long DesafianteId;
        public long DesafiadoId;
        public double CentroX;
        public double CentroZ;
        public int ContagemRegressivaSegundos = RegrasPvp.TempoContagemRegressivaDueloSegundos;
    }
}
