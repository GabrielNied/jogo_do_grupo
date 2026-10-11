namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client passa a ignorar o dono de um personagem, pelo nome. Vale para a conta e desfaz a
    /// amizade e os pedidos entre os dois. Id: <see cref="SocialMessageIds.IgnorarJogador"/>.
    /// </summary>
    [System.Serializable]
    public class IgnorarJogador
    {
        /// <summary>Nome de um personagem do alvo (o servidor não diferencia maiúsculas).</summary>
        public string Nome = "";
    }
}
