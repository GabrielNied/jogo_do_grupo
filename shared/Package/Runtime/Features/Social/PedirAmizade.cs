namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client pede amizade ao dono de um personagem, pelo nome. A amizade vale para a conta; quem
    /// pede vem da sessão autenticada. Se o alvo já tinha pedido amizade a quem pede, o servidor
    /// trata como aceite. Id: <see cref="SocialMessageIds.PedirAmizade"/>.
    /// </summary>
    [System.Serializable]
    public class PedirAmizade
    {
        /// <summary>Nome de um personagem do alvo (o servidor não diferencia maiúsculas).</summary>
        public string Nome = "";
    }
}
