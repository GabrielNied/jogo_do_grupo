namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client pede ao servidor uma esquiva. É só a intenção: o servidor cobra o vigor e valida
    /// o deslocamento; o client pode antecipar o movimento e corrige se vier
    /// <see cref="EsquivaRecusada"/>.
    /// Id: <see cref="CombateMessageIds.IntencaoDeEsquiva"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeEsquiva
    {
        /// <summary>
        /// Direção do deslocamento no plano do chão, em graus, na mesma convenção de
        /// <see cref="IntencaoDeAtaque.DirecaoY"/> (0 aponta para +Z e cresce para +X).
        /// </summary>
        public float DirecaoY = 0f;

        /// <summary>
        /// Número crescente gerado pelo client; volta em <see cref="EsquivaRecusada.Sequencia"/>
        /// quando o pedido é recusado.
        /// </summary>
        public int Sequencia = 0;
    }
}
