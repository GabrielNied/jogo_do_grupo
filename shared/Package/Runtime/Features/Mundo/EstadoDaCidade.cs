namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor envia ao client logo após este entrar na cidade.
    /// Contém a posição do próprio jogador e de todos os outros presentes no momento.
    /// Id: <see cref="MundoMessageIds.EstadoDaCidade"/>.
    /// </summary>
    [System.Serializable]
    public class EstadoDaCidade
    {
        /// <summary>O próprio jogador.</summary>
        public JogadorVisivel Eu = new JogadorVisivel();

        /// <summary>Demais jogadores presentes na cidade no momento do snapshot.</summary>
        public JogadorVisivel[] Outros = System.Array.Empty<JogadorVisivel>();
    }
}
