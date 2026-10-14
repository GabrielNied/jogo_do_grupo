namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Valores ajustáveis da redistribuição de talentos e dos perfis de build.
    /// Fórmulas finas e números definitivos dependem da #627; aqui moram os valores de teste.
    /// </summary>
    public static class ConfigRedistribuicao
    {
        /// <summary>Custo padrão em coroas para redistribuir todos os pontos da árvore.</summary>
        public const long CustoPadraoCoroas = 100L;

        /// <summary>Limite máximo de perfis de build salvos por personagem.</summary>
        public const int LimitePerfis = 5;

        /// <summary>Tamanho máximo permitido para o nome de um perfil de build.</summary>
        public const int TamanhoMaximoNomePerfil = 32;
    }
}
