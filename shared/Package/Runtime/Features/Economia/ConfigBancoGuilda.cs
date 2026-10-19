namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Constantes e valores de balanceamento do Banco de Guilda (#338).
    /// </summary>
    public static class ConfigBancoGuilda
    {
        /// <summary>Quantidade de casas por aba no banco da guilda.</summary>
        public const int CasasPorAba = 40;

        /// <summary>Quantidade de abas disponíveis no banco da guilda.</summary>
        public const int AbasDisponiveis = 2;

        /// <summary>Capacidade total máxima de itens no cofre da guilda (80 itens).</summary>
        public const int CapacidadeMaxima = CasasPorAba * AbasDisponiveis;

        /// <summary>Coordenada X do Cofre / Banqueiro da Guilda na cidade.</summary>
        public const float BancoGuildaX = 3f;

        /// <summary>Coordenada Z do Cofre / Banqueiro da Guilda na cidade.</summary>
        public const float BancoGuildaZ = -14f;

        /// <summary>Distância máxima (em metros) para interagir com o Banco da Guilda.</summary>
        public const float RaioInteracaoMetros = 4f;

        public static bool PertoDoBancoGuilda(float x, float z)
        {
            var dx = x - BancoGuildaX;
            var dz = z - BancoGuildaZ;
            return (dx * dx) + (dz * dz) <= RaioInteracaoMetros * RaioInteracaoMetros;
        }
    }
}
