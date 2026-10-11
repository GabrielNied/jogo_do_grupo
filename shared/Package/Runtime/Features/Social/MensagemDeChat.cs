namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor entrega uma mensagem de chat aceita a quem deve recebê-la, inclusive ao
    /// remetente (é a confirmação dele). Mostre <see cref="Texto"/> como texto puro, com as
    /// marcas de formatação desligadas.
    /// Id: <see cref="SocialMessageIds.MensagemDeChat"/>.
    /// </summary>
    [System.Serializable]
    public class MensagemDeChat
    {
        public CanalDeChat Canal = CanalDeChat.Cidade;
        public long RemetenteId = 0;
        public string NomeDoRemetente = "";

        /// <summary>Só no sussurro: nome de quem recebe. Vazio nos outros canais.</summary>
        public string NomeDoAlvo = "";

        /// <summary>Texto já limpo pelo servidor.</summary>
        public string Texto = "";

        /// <summary>Hora do servidor em que a mensagem foi aceita (milissegundos Unix, UTC).</summary>
        public long HoraUnixMs = 0;
    }
}
