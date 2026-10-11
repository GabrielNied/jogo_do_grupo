namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Client envia ao servidor a posição desejada a cada quadro (ou tick) de movimento.
    /// O servidor valida e corrige via <see cref="PosicaoJogador"/>.
    /// Id: <see cref="MundoMessageIds.IntencaoDeMovimento"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeMovimento
    {
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float RotacaoY = 0f;

        /// <summary>
        /// Número de sequência crescente gerado pelo client; o servidor devolve o mesmo
        /// valor em <see cref="PosicaoJogador.Sequencia"/> para o client reconciliar a predição.
        /// </summary>
        public int Sequencia = 0;
    }
}
