namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um inimigo mudou de posição, estado, alvo ou vida
    /// (a vida volta ao máximo quando ele desiste da perseguição). O dano de um golpe chega
    /// antes, pelo <c>AtaqueAcertou</c> do Combate. O client descarta a mensagem cuja
    /// <see cref="Sequencia"/> não é maior que a última que aplicou para o mesmo inimigo.
    /// Id: <see cref="MobsMessageIds.InimigoMudou"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoMudou
    {
        public long InimigoId = 0;

        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        /// <summary>Para onde ele olha, em graus (0 aponta para +Z e cresce para +X).</summary>
        public float RotacaoY = 0f;

        public EstadoDeInimigo Estado = EstadoDeInimigo.Repouso;

        /// <summary>
        /// Personagem que ele persegue ou ataca; <see cref="RegrasInimigo.SemAlvo"/> se nenhum.
        /// </summary>
        public long AlvoId = 0;

        public int Vida = 0;

        /// <summary>Cresce a cada mensagem do mesmo inimigo; recomeça quando ele reaparece.</summary>
        public int Sequencia = 0;
    }
}
