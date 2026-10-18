namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → Solicitante: notificação de recusa ao tentar definir um marcador tático.
    /// </summary>
    public sealed class DefinirMarcadorRecusado
    {
        public CodigoErroMarcador Motivo { get; set; }
    }
}
