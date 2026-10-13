namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que o golpe de um inimigo encostou em um personagem. É
    /// só para o client segurar a pose de quem bateu por um instante
    /// (<c>RegrasReacao.PausaVisualNoAcertoMs</c>); a vida do alvo muda por outro caminho e o
    /// relógio do servidor não para. Id: <see cref="MobsMessageIds.InimigoAcertou"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoAcertou
    {
        public long InimigoId = 0;

        /// <summary>Quem levou o golpe.</summary>
        public long AlvoId = 0;
    }
}
