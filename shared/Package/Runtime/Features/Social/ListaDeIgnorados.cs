namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor envia a lista completa de ignorados ao entrar no jogo e a cada mudança. Só o dono
    /// da lista recebe; quem foi ignorado não fica sabendo.
    /// Id: <see cref="SocialMessageIds.ListaDeIgnorados"/>.
    /// </summary>
    [System.Serializable]
    public class ListaDeIgnorados
    {
        /// <summary>Ignorados em ordem de nome. Nunca nulo.</summary>
        public JogadorIgnorado[] Ignorados = System.Array.Empty<JogadorIgnorado>();
    }
}
