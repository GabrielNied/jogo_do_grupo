namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa quem pediu que a operação de amigos ou de ignorados não valeu. Nada mudou.
    /// Id: <see cref="SocialMessageIds.OperacaoDeAmizadeRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class OperacaoDeAmizadeRecusada
    {
        /// <summary>Um dos códigos de <see cref="CodigosErroSocial"/>.</summary>
        public string Codigo = "";
    }
}
