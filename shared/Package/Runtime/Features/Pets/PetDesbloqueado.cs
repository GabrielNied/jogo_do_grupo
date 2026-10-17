namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Notificação de que um novo pet foi registrado na conta.
    /// Mensagem servidor -> client. Id: <see cref="PetsMessageIds.PetDesbloqueado"/>.
    /// </summary>
    [System.Serializable]
    public class PetDesbloqueado
    {
        public int Especie;
        public string Nome = string.Empty;
    }
}
