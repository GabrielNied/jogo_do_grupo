namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Client confirma que aplicou a <see cref="PosicaoForcada"/>.
    /// Id: <see cref="MundoMessageIds.ConfirmarPosicaoForcada"/>.
    /// </summary>
    [System.Serializable]
    public class ConfirmarPosicaoForcada
    {
        /// <summary>A <see cref="PosicaoForcada.Sequencia"/> aplicada.</summary>
        public int Sequencia = 0;
    }
}
