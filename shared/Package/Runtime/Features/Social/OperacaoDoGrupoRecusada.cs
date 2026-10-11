namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa quem pediu que a operação de grupo não valeu. Nada mudou no grupo.
    /// Id: <see cref="SocialMessageIds.OperacaoDoGrupoRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class OperacaoDoGrupoRecusada
    {
        /// <summary>Um dos códigos de <see cref="CodigosErroSocial"/>.</summary>
        public string Codigo = "";
    }
}
