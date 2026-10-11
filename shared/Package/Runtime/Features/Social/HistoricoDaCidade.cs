namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor envia a quem entra na cidade as últimas mensagens do canal Cidade, da mais
    /// antiga para a mais nova. Grupo e sussurro não têm histórico.
    /// Id: <see cref="SocialMessageIds.HistoricoDaCidade"/>.
    /// </summary>
    [System.Serializable]
    public class HistoricoDaCidade
    {
        /// <summary>Nunca nulo; vazio quando ninguém falou há pouco.</summary>
        public MensagemDeChat[] Mensagens = System.Array.Empty<MensagemDeChat>();
    }
}
