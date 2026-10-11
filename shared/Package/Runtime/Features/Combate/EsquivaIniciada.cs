namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área (inclusive o dono) que um personagem começou a esquivar,
    /// para tocar a animação. A posição continua chegando pelas mensagens do Mundo.
    /// Id: <see cref="CombateMessageIds.EsquivaIniciada"/>.
    /// </summary>
    [System.Serializable]
    public class EsquivaIniciada
    {
        public long PersonagemId = 0;

        /// <summary>Direção do deslocamento, em graus (convenção de <see cref="IntencaoDeEsquiva.DirecaoY"/>).</summary>
        public float DirecaoY = 0f;

        /// <summary>Milissegundos que a esquiva dura.</summary>
        public int DuracaoMs = 0;
    }
}
