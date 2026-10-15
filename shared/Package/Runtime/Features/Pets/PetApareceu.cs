namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Servidor avisa quem vê o dono que o pet dele está no campo de visão (foi chamado, ou o dono
    /// entrou no interesse de quem recebe). O pet é identificado pelo <see cref="DonoId"/>: há um
    /// por dono. Id: <see cref="PetsMessageIds.PetApareceu"/>.
    /// </summary>
    [System.Serializable]
    public class PetApareceu
    {
        public long DonoId = 0;
        public EspecieDePet Especie = EspecieDePet.CervoJovem;
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        /// <summary>Para onde ele olha, em graus (0 aponta para +Z e cresce para +X).</summary>
        public float RotacaoY = 0f;

        public bool Andando = false;

        /// <summary>A sequência atual do pet; as <see cref="PetMudou"/> seguintes são maiores.</summary>
        public int Sequencia = 0;
    }
}
