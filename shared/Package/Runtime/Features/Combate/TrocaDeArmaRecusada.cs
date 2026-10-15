namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa o dono que a troca não vale. Nada mudou.
    /// Id: <see cref="CombateMessageIds.TrocaDeArmaRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class TrocaDeArmaRecusada
    {
        /// <summary>O mesmo valor de <see cref="IntencaoDeTrocaDeArma.Sequencia"/> do pedido.</summary>
        public int Sequencia = 0;

        /// <summary>
        /// Motivo: <see cref="CodigosErroCombate.AcaoEmAndamento"/>,
        /// <see cref="CodigosErroCombate.VoceEstaCaido"/> ou
        /// <see cref="CodigosErroCombate.VoceEstaAtordoado"/>.
        /// </summary>
        public string Codigo = "";
    }
}
