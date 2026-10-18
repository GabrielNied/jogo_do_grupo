namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Participantes: duelo finalizado com vencedor e motivo.
    /// </summary>
    [System.Serializable]
    public class DueloEncerrado
    {
        public long VencedorId;
        public long PerdedorId;
        public MotivoDueloEncerramento Motivo;
    }
}
