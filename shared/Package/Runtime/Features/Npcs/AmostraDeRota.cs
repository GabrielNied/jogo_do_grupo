namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Onde um NPC de rota está num instante (#1069).</summary>
    public readonly struct AmostraDeRota
    {
        public readonly float X;
        public readonly float Z;

        /// <summary>Direção do rosto, em graus em torno de Y (0 = +Z).</summary>
        public readonly float RotacaoY;

        public readonly bool Parado;

        /// <summary>Parado num ponto de conversa.</summary>
        public readonly bool Conversando;

        /// <summary>Segundos desde que parou (0 enquanto anda).</summary>
        public readonly float TempoNaPausa;

        public AmostraDeRota(
            float x,
            float z,
            float rotacaoY,
            bool parado,
            bool conversando,
            float tempoNaPausa
        )
        {
            X = x;
            Z = z;
            RotacaoY = rotacaoY;
            Parado = parado;
            Conversando = conversando;
            TempoNaPausa = tempoNaPausa;
        }
    }
}
