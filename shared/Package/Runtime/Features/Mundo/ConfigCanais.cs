namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Valores de balanceamento e capacidade dos canais do mundo persistente (ADR 0045).</summary>
    public static class ConfigCanais
    {
        /// <summary>Lotação máxima de jogadores por canal (ADR 0045 / #150).</summary>
        public const int CapacidadePorCanal = 80;

        /// <summary>Quantidade padrão de canais que o processo de servidor mantém ativos.</summary>
        public const int QuantidadeCanaisPadrao = 3;

        /// <summary>Identificador do canal inicial (1-based).</summary>
        public const int CanalMinimo = 1;
    }
}
