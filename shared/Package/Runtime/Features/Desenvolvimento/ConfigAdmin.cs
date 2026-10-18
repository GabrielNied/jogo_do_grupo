namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>Limites da tela de admin, para não aceitar pedido absurdo.</summary>
    public static class ConfigAdmin
    {
        /// <summary>Itens por pedido de <see cref="AcaoDeAdmin.GerarItem"/>.</summary>
        public const int QuantidadeMaximaDeItens = 20;

        /// <summary>Inimigos por pedido de <see cref="AcaoDeAdmin.SpawnarInimigo"/>.</summary>
        public const int QuantidadeMaximaDeInimigos = 10;

        /// <summary>Módulo do ajuste de experiência, igual ao do comando <c>xp</c>.</summary>
        public const long ExperienciaMaxima = ComandosDeTeste.ExperienciaMaxima;

        /// <summary>Módulo do ajuste de coroas por pedido.</summary>
        public const long CoroasMaximas = 1000000;

        /// <summary>Essências de cada tipo por pedido.</summary>
        public const int EssenciasMaximas = 100;
    }
}
