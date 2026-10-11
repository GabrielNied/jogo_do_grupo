namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client pede ao servidor um ataque com a arma equipada. É só a intenção: acerto, dano e
    /// custo são decididos pelo servidor, que usa a posição que ele tem do personagem.
    /// Id: <see cref="CombateMessageIds.IntencaoDeAtaque"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeAtaque
    {
        public TipoDeAtaque Tipo = TipoDeAtaque.Leve;

        /// <summary>
        /// Direção da mira no plano do chão, em graus, na mesma convenção de
        /// <c>IntencaoDeMovimento.RotacaoY</c> (0 aponta para +Z e cresce para +X).
        /// </summary>
        public float DirecaoY = 0f;

        /// <summary>
        /// Número crescente gerado pelo client; volta em <see cref="AtaqueRecusado.Sequencia"/>
        /// quando o pedido é recusado.
        /// </summary>
        public int Sequencia = 0;
    }
}
