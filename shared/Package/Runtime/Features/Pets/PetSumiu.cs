namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Servidor avisa que o pet saiu do campo de visão ou deixou de existir (dispensado, dono
    /// desconectou ou entrou numa caverna). O client remove o modelo. Id:
    /// <see cref="PetsMessageIds.PetSumiu"/>.
    /// </summary>
    [System.Serializable]
    public class PetSumiu
    {
        public long DonoId = 0;
    }
}
