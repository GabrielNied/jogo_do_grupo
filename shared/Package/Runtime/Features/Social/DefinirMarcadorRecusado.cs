namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → Solicitante: notificação de recusa ao tentar definir um marcador tático.
    /// </summary>
    [System.Serializable]
    public sealed class DefinirMarcadorRecusado
    {
        public CodigoErroMarcador Motivo;
    }
}
