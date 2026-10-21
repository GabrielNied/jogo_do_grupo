namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Valores ajustáveis da vida na cidade: guardas e moradores ambiente (#1069).</summary>
    public static class ConfigPopulacaoDaCidade
    {
        /// <summary>Velocidade de caminhada dos moradores, em m/s.</summary>
        public const float VelocidadeMorador = 1.2f;

        /// <summary>Velocidade de patrulha dos guardas, em m/s.</summary>
        public const float VelocidadeGuarda = 1.5f;

        /// <summary>Distância do jogador em que o balão de fala pode aparecer, em metros.</summary>
        public const float RaioDoBalao = 8f;

        /// <summary>Quanto tempo um balão fica na tela, em segundos.</summary>
        public const float DuracaoDoBalao = 4f;

        /// <summary>Espera mínima entre dois balões do mesmo NPC para o mesmo jogador, em segundos.</summary>
        public const float EsperaEntreBaloes = 25f;

        /// <summary>Além desta distância do jogador o NPC ambiente nem é desenhado nem animado, em metros.</summary>
        public const float RaioDeDesenho = 90f;

        /// <summary>Tamanho máximo de uma fala (cabe num balão de duas linhas).</summary>
        public const int TamanhoMaximoDaFala = 60;
    }
}
