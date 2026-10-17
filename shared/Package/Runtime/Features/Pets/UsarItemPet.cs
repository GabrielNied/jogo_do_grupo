namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Intenção de usar item de pet no inventário para registrá-lo na conta e chamá-lo.
    /// Mensagem client -> servidor. Id: <see cref="PetsMessageIds.UsarItemPet"/>.
    /// </summary>
    [System.Serializable]
    public class UsarItemPet
    {
        public long InstanciaId;
    }
}
