namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Valores ajustáveis do movimento suave dos inimigos (regra 17): o atraso de render e os
    /// limites da extrapolação. O servidor manda a posição a cerca de 10 Hz.
    /// </summary>
    internal static class ConfigMovimentoDoInimigo
    {
        /// <summary>
        /// Quanto a pose mostrada fica atrás do último pacote, em segundos. Um intervalo de envio
        /// (100 ms) mais folga para a variação de chegada: sempre há dois pacotes para interpolar.
        /// </summary>
        public const float AtrasoDeRenderSegundos = 0.15f;

        /// <summary>
        /// Quanto, além do último pacote, a pose ainda segue na velocidade dele, em segundos
        /// (cobre um pacote perdido). Depois disso ela para e espera.
        /// </summary>
        public const float ExtrapolacaoMaximaSegundos = 0.1f;

        /// <summary>
        /// Deslocamento horizontal (m) entre dois pacotes acima do qual é teletransporte (volta
        /// ao ponto de origem, reaparição): a pose pula, sem deslizar nem contar como velocidade.
        /// </summary>
        public const float SaltoDeTeletransporteMetros = 5f;

        /// <summary>Quantos pacotes o buffer guarda no máximo (descarta os mais antigos).</summary>
        public const int PacotesNoBuffer = 32;
    }
}
