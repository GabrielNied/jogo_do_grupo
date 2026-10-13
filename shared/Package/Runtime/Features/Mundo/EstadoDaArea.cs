namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor envia ao personagem após uma transição de área, com o destino e quem já está nele.
    /// Id: <see cref="MundoMessageIds.EstadoDaArea"/>.
    /// </summary>
    [System.Serializable]
    public class EstadoDaArea
    {
        public string Area = string.Empty;
        public JogadorVisivel Eu = new JogadorVisivel();
        public JogadorVisivel[] Outros = System.Array.Empty<JogadorVisivel>();
    }
}
