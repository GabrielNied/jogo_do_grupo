namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client pede ao servidor para convidar um personagem para o grupo. Quem convida vem da
    /// sessão autenticada; o client nunca envia ids de outros jogadores, só o nome.
    /// Id: <see cref="SocialMessageIds.ConvidarParaGrupo"/>.
    /// </summary>
    [System.Serializable]
    public class ConvidarParaGrupo
    {
        /// <summary>Nome do personagem convidado (o servidor não diferencia maiúsculas).</summary>
        public string Nome = "";
    }
}
