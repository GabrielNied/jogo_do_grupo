namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor envia a todos os presentes quando um novo jogador entra na cidade.
    /// Id: <see cref="MundoMessageIds.JogadorEntrou"/>.
    /// </summary>
    [System.Serializable]
    public class JogadorEntrou
    {
        public JogadorVisivel Jogador = new JogadorVisivel();
    }
}
