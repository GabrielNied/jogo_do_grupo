namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Servidor recusou chamar ou dispensar o pet. <see cref="Codigo"/> é um de
    /// <see cref="CodigosErroPets"/>. Id: <see cref="PetsMessageIds.PetRecusado"/>.
    /// </summary>
    [System.Serializable]
    public class PetRecusado
    {
        public string Codigo = "";
    }
}
