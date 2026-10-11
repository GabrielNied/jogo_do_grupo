namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Client pede ao servidor outras velocidades, para testar no editor (o equivalente ao
    /// comando de GM <c>.modify speed</c> do WoW). O servidor só aceita quando está configurado
    /// para isso (desenvolvimento) e dentro dos limites de <see cref="RegrasVelocidade"/>; se
    /// aceitar, responde com <see cref="VelocidadesDeMovimento"/>. Se recusar, não responde.
    /// Id: <see cref="MundoMessageIds.PedirVelocidadeDeTeste"/>.
    /// </summary>
    [System.Serializable]
    public class PedirVelocidadeDeTeste
    {
        public float Andar = RegrasVelocidade.AndarPadrao;
        public float Correr = RegrasVelocidade.CorrerPadrao;
    }
}
