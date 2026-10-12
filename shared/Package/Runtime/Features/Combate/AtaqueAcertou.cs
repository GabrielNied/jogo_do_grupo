namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área que um golpe acertou. Cada alvo aparece uma vez só: o
    /// mesmo golpe não acerta o mesmo alvo duas vezes.
    /// Id: <see cref="CombateMessageIds.AtaqueAcertou"/>.
    /// </summary>
    [System.Serializable]
    public class AtaqueAcertou
    {
        public long AtacanteId = 0;

        /// <summary>Alvos atingidos no momento de acerto. Nunca nulo.</summary>
        public AcertoDeAtaque[] Acertos = System.Array.Empty<AcertoDeAtaque>();

        /// <summary>Tipo do golpe que acertou (o client não adivinha pelo último ataque iniciado).</summary>
        public TipoDeAtaque Tipo = TipoDeAtaque.Leve;

        /// <summary>
        /// <see cref="IntencaoDeAtaque.Sequencia"/> do pedido que gerou este golpe; liga o acerto
        /// ao golpe certo mesmo com leve e pesado na fila.
        /// </summary>
        public int Sequencia = 0;

        /// <summary>Passo da cadeia de leves do golpe (0 a 2), o mesmo de <see cref="AtaqueIniciado.Passo"/>.</summary>
        public int Passo = 0;
    }
}
