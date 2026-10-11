namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Client confirma que aplicou as velocidades recebidas em <see cref="VelocidadesDeMovimento"/>.
    /// Id: <see cref="MundoMessageIds.ConfirmarVelocidade"/>.
    /// </summary>
    [System.Serializable]
    public class ConfirmarVelocidade
    {
        /// <summary>A <see cref="VelocidadesDeMovimento.Sequencia"/> aplicada.</summary>
        public int Sequencia = 0;
    }
}
