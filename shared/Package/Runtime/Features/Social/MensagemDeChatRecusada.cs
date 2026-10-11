namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa o remetente que a mensagem não valeu. Ela não foi entregue a ninguém.
    /// Id: <see cref="SocialMessageIds.MensagemDeChatRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class MensagemDeChatRecusada
    {
        /// <summary>Um dos códigos de chat de <see cref="CodigosErroSocial"/>.</summary>
        public string Codigo = "";

        /// <summary>
        /// Só em <see cref="CodigosErroSocial.Silenciado"/>: segundos que faltam para o silêncio
        /// acabar. Zero nos outros códigos.
        /// </summary>
        public int SegundosRestantes = 0;
    }
}
