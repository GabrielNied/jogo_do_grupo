namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa o atacante que o pedido de ataque não vale. Nada foi gasto.
    /// Id: <see cref="CombateMessageIds.AtaqueRecusado"/>.
    /// </summary>
    [System.Serializable]
    public class AtaqueRecusado
    {
        /// <summary>O mesmo valor de <see cref="IntencaoDeAtaque.Sequencia"/> do pedido recusado.</summary>
        public int Sequencia = 0;

        /// <summary>
        /// Motivo: <see cref="CodigosErroCombate.StaminaInsuficiente"/>,
        /// <see cref="CodigosErroCombate.AcaoEmAndamento"/> ou
        /// <see cref="CodigosErroCombate.VoceEstaCaido"/>.
        /// </summary>
        public string Codigo = "";
    }
}
