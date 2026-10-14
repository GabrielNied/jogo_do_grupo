namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa o dono que o pedido de esquiva não vale. Nada foi gasto e a posição não
    /// muda: o client desfaz o movimento que antecipou.
    /// Id: <see cref="CombateMessageIds.EsquivaRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class EsquivaRecusada
    {
        /// <summary>O mesmo valor de <see cref="IntencaoDeEsquiva.Sequencia"/> do pedido recusado.</summary>
        public int Sequencia = 0;

        /// <summary>
        /// Motivo: <see cref="CodigosErroCombate.StaminaInsuficiente"/>,
        /// <see cref="CodigosErroCombate.AcaoEmAndamento"/>,
        /// <see cref="CodigosErroCombate.VoceEstaCaido"/> ou
        /// <see cref="CodigosErroCombate.VoceEstaAtordoado"/> (os mesmos do ataque).
        /// </summary>
        public string Codigo = "";
    }
}
