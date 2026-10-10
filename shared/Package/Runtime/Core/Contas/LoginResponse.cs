namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>
    /// Servidor responde (200) a <see cref="LoginRequest"/>.
    /// O client manda <see cref="Token"/> nas rotas protegidas no header
    /// <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    [System.Serializable]
    public class LoginResponse
    {
        /// <summary>JWT da conta.</summary>
        public string Token = "";

        /// <summary>Quando o token expira, em segundos Unix (UTC).</summary>
        public long ExpiraEmUnix;
    }
}
