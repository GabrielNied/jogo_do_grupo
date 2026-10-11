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
    }
}
