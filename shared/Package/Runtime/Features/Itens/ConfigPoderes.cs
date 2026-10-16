namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Valores de teste dos poderes e do set de exemplo (regra 17), até o balanceamento final.
    /// Mudam sem mexer nas regras.
    /// </summary>
    public static class ConfigPoderes
    {
        /// <summary>Tempo da marca que a investida deixa nos alvos.</summary>
        public const int MarcaDaInvestidaMs = 6000;

        /// <summary>Cargas máximas de Embalo (golpes seguidos), quanto duram e o dano extra de cada uma.</summary>
        public const int EmbaloMaximoDeCargas = 3;
        public const int EmbaloDuracaoMs = 5000;
        public const double EmbaloBonusPorCarga = 0.10;

        /// <summary>Quanto dura o Impulso (esquiva) e o dano extra da investida que o consome.</summary>
        public const int ImpulsoDuracaoMs = 4000;
        public const double ImpulsoBonus = 0.40;

        /// <summary>Recarga tirada ao acertar um alvo marcado e a folga entre duas ativações.</summary>
        public const int CacadorReducaoDeRecargaMs = 2000;
        public const int CacadorIntervaloMs = 4000;

        /// <summary>Set de exemplo, marco de 2 peças: cargas de Ritmo, duração e dano extra por carga.</summary>
        public const int RitmoMaximoDeCargas = 2;
        public const int RitmoDuracaoMs = 6000;
        public const double RitmoBonusPorCarga = 0.08;

        /// <summary>Set de exemplo, marco de 4 peças: recarga tirada e folga entre ativações.</summary>
        public const int FolegoReducaoDeRecargaMs = 1500;
        public const int FolegoIntervaloMs = 4000;
    }
}
