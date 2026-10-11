namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Representa um jogador visível na área da cidade: posição e orientação.
    /// Usado em <see cref="EstadoDaCidade"/>, <see cref="JogadorEntrou"/> e <see cref="PosicaoJogador"/>.
    /// </summary>
    [System.Serializable]
    public class JogadorVisivel
    {
        public long PersonagemId = 0;
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float RotacaoY = 0f;
    }
}
