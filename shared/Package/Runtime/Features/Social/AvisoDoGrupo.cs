namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa que algo aconteceu no grupo ou com um convite, para o client mostrar uma
    /// mensagem. Não muda a lista: a lista nova chega em <see cref="ListaDoGrupo"/>.
    /// Id: <see cref="SocialMessageIds.AvisoDoGrupo"/>.
    /// </summary>
    [System.Serializable]
    public class AvisoDoGrupo
    {
        public TipoDeAvisoDoGrupo Tipo = TipoDeAvisoDoGrupo.ConviteRecusado;

        /// <summary>O personagem de quem o aviso fala (quem entrou, saiu, recusou...).</summary>
        public long PersonagemId = 0;

        /// <summary>Nome desse personagem, para mostrar sem outra consulta.</summary>
        public string Nome = "";
    }
}
