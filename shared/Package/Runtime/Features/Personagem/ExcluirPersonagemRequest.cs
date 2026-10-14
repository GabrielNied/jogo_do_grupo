namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasPersonagem.Excluir"/> (com token).
    /// </summary>
    [System.Serializable]
    public class ExcluirPersonagemRequest
    {
        /// <summary>Nome do personagem digitado para confirmar a exclusão.</summary>
        public string ConfirmacaoNome = "";
    }
}
