namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor avisa o client das velocidades do personagem (m/s), que são autoritativas no
    /// servidor. O client aplica e responde <see cref="ConfirmarVelocidade"/> com a mesma
    /// <see cref="Sequencia"/>; até a confirmação, o servidor aceita a maior entre a velocidade
    /// antiga e a nova. Modelo do WoW (FORCE_RUN_SPEED_CHANGE e o ACK).
    /// Id: <see cref="MundoMessageIds.VelocidadesDeMovimento"/>.
    /// </summary>
    [System.Serializable]
    public class VelocidadesDeMovimento
    {
        public float Andar = RegrasVelocidade.AndarPadrao;
        public float Correr = RegrasVelocidade.CorrerPadrao;

        /// <summary>Cresce a cada mudança; o client devolve na confirmação.</summary>
        public int Sequencia = 0;
    }
}
