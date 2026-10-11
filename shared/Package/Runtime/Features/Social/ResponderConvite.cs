namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client responde a um convite de grupo que recebeu em <see cref="ConviteRecebido"/>.
    /// Id: <see cref="SocialMessageIds.ResponderConvite"/>.
    /// </summary>
    [System.Serializable]
    public class ResponderConvite
    {
        /// <summary>O mesmo valor de <see cref="ConviteRecebido.ConviteId"/>.</summary>
        public long ConviteId = 0;

        /// <summary>Verdadeiro aceita o convite; falso recusa.</summary>
        public bool Aceitar = false;
    }
}
