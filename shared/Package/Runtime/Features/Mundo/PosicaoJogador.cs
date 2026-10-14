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
        /// Sequência crescente por jogador emissor no servidor (ou espelho de
        /// <see cref="IntencaoDeMovimento.Sequencia"/> em predição local).
        /// </summary>
        public int Sequencia = 0;

        /// <summary>
        /// Carimbo de tempo do servidor em milissegundos (<c>Environment.TickCount64</c>).
        /// Usado pelo client para alinhar o buffer de interpolação e descartar pacotes velhos.
        /// </summary>
        public long TempoServidorMs = 0;
    }
}
