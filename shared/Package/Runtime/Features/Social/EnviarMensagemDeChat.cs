namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client envia uma mensagem de chat. Quem falou vem da sessão autenticada, nunca de um
    /// campo; o servidor limpa e valida o texto de novo (<see cref="RegrasChat"/>).
    /// Id: <see cref="SocialMessageIds.EnviarMensagemDeChat"/>.
    /// </summary>
    [System.Serializable]
    public class EnviarMensagemDeChat
    {
        public CanalDeChat Canal = CanalDeChat.Cidade;

        /// <summary>Texto puro; o servidor não interpreta formatação.</summary>
        public string Texto = "";

        /// <summary>
        /// Só no <see cref="CanalDeChat.Sussurro"/>: nome do personagem que recebe (o servidor
        /// não diferencia maiúsculas). Vazio nos outros canais.
        /// </summary>
        public string NomeDoAlvo = "";
    }
}
