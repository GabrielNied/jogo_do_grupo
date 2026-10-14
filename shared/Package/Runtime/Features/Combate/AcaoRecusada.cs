namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa que um pedido do estado caído (<see cref="ComecarALevantar"/> ou
    /// <see cref="VoltarParaACidade"/>) não vale. Nada mudou.
    /// Id: <see cref="CombateMessageIds.AcaoRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class AcaoRecusada
    {
        /// <summary>
        /// Motivo: <see cref="CodigosErroCombate.VoceEstaCaido"/>,
        /// <see cref="CodigosErroCombate.AlvoNaoEstaCaido"/>,
        /// <see cref="CodigosErroCombate.LongeDemais"/>,
        /// <see cref="CodigosErroCombate.JaEstaSendoLevantado"/>,
        /// <see cref="CodigosErroCombate.NaoEstaCaido"/>,
        /// <see cref="CodigosErroCombate.AindaNaoPodeVoltar"/> ou
        /// <see cref="CodigosErroCombate.VoceEstaAtordoado"/>.
        /// </summary>
        public string Codigo = "";
    }
}
