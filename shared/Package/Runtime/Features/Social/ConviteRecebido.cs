namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa o convidado de que chegou um convite de grupo. Responda com
    /// <see cref="ResponderConvite"/> antes de a validade acabar.
    /// Id: <see cref="SocialMessageIds.ConviteRecebido"/>.
    /// </summary>
    [System.Serializable]
    public class ConviteRecebido
    {
        public long ConviteId = 0;
        public string NomeDoConvidante = "";

        /// <summary>Segundos que o convite ainda vale, contados de quando foi enviado.</summary>
        public int ValidadeSegundos = 0;
    }
}
