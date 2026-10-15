namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Client pede o pet. Se já há um chamado, nada muda (um pet por vez). Id:
    /// <see cref="PetsMessageIds.ChamarPet"/>.
    /// </summary>
    [System.Serializable]
    public class ChamarPet
    {
        public EspecieDePet Especie = EspecieDePet.CervoJovem;
    }
}
