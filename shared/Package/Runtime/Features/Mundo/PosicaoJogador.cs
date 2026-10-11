namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor envia a posição autoritativa de um jogador.
    /// Quando <see cref="PersonagemId"/> é o do próprio client, serve para corrigir a predição;
    /// quando é de outro jogador, serve como réplica.
    /// Id: <see cref="MundoMessageIds.PosicaoJogador"/>.
    /// </summary>
    [System.Serializable]
    public class PosicaoJogador
    {
        public long PersonagemId = 0;
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float RotacaoY = 0f;

        /// <summary>
        /// Sequência espelhada de <see cref="IntencaoDeMovimento.Sequencia"/>; zero em réplicas de
        /// outros jogadores.
        /// </summary>
        public int Sequencia = 0;
    }
}
