namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor manda o jogador para uma posição: é a única correção da própria posição que o
    /// client aceita (ADR 0024). Vem em teleporte, nascimento depois da morte, empurrão de boss e
    /// trapaça grosseira; nunca como reconciliação do movimento do dia a dia. O client aplica e
    /// responde <see cref="ConfirmarPosicaoForcada"/>; até lá, o servidor ignora o movimento
    /// (que ainda é de antes do teleporte). Modelo do WoW (MSG_MOVE_TELEPORT e o ACK).
    /// Id: <see cref="MundoMessageIds.PosicaoForcada"/>.
    /// </summary>
    [System.Serializable]
    public class PosicaoForcada
    {
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float RotacaoY = 0f;

        /// <summary>Cresce a cada posição forçada; o client devolve na confirmação.</summary>
        public int Sequencia = 0;
    }
}
