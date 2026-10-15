namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Servidor avisa que o pet andou ou parou, no mesmo jeito dos mobs (entrega sequenciada por
    /// pet). O client descarta a mensagem cuja <see cref="Sequencia"/> não é maior que a última
    /// que aplicou. Id: <see cref="PetsMessageIds.PetMudou"/>.
    /// </summary>
    [System.Serializable]
    public class PetMudou
    {
        public long DonoId = 0;
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float RotacaoY = 0f;
        public bool Andando = false;

        /// <summary>Cresce a cada mensagem do mesmo pet; recomeça quando ele é chamado de novo.</summary>
        public int Sequencia = 0;
    }
}
