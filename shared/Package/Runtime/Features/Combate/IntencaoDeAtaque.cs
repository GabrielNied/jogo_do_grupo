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
        /// Elevação vertical da mira em graus (Pitch), onde 0 é paralelo ao chão, positivo aponta
        /// para cima (+Y) e negativo aponta para baixo (-Y). Clampeado no servidor a
        /// <see cref="RegrasProjetil.ElevacaoMinimaGraus"/> e <see cref="RegrasProjetil.ElevacaoMaximaGraus"/>.
        /// </summary>
        public float ElevacaoGraus = 0f;

        /// <summary>
        /// Número crescente gerado pelo client; volta em <see cref="AtaqueRecusado.Sequencia"/>
        /// quando o pedido é recusado.
        /// </summary>
        public int Sequencia = 0;

        /// <summary>
        /// Relógio do servidor (<c>Environment.TickCount64</c> no host) que o client acreditava
        /// ter ao pedir o golpe; o servidor rebobina até <see cref="RegrasCompensacaoDeLag.JanelaMs"/>
        /// para medir o volume. Zero = sem referência (comportamento anterior).
        /// </summary>
        public long TempoVistoMs = 0;
    }
}
