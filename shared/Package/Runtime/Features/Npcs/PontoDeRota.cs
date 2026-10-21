namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Um ponto de uma <see cref="RotaDeNpc"/>: onde o NPC para e por quanto tempo (#1069).</summary>
    public sealed class PontoDeRota
    {
        public readonly float X;
        public readonly float Z;
        public readonly float PausaSegundos;

        /// <summary>A pausa neste ponto é uma conversa (o balão alterna com o par).</summary>
        public readonly bool Conversa;

        /// <summary>Se <see cref="TemOlhar"/>, o NPC olha para (<see cref="OlharX"/>, <see cref="OlharZ"/>) parado.</summary>
        public readonly bool TemOlhar;
        public readonly float OlharX;
        public readonly float OlharZ;

        public PontoDeRota(
            float x,
            float z,
            float pausaSegundos = 0f,
            bool conversa = false,
            bool temOlhar = false,
            float olharX = 0f,
            float olharZ = 0f
        )
        {
            X = x;
            Z = z;
            PausaSegundos = pausaSegundos < 0f ? 0f : pausaSegundos;
            Conversa = conversa;
            TemOlhar = temOlhar;
            OlharX = olharX;
            OlharZ = olharZ;
        }
    }
}
