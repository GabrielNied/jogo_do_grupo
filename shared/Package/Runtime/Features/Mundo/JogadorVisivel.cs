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

        /// <summary>Nome do personagem, preenchido pelo servidor (vazio se desconhecido).</summary>
        public string Nome = string.Empty;

        /// <summary>
        /// Raça como número (o valor de <c>Raca</c> de Personagem; 0 se desconhecida). Número
        /// para o Mundo não depender do enum de outra funcionalidade.
        /// </summary>
        public int Raca = 0;
    }
}
