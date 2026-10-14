namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Valores ajustáveis de interpolação e movimento suave de jogadores remotos (regra 17).
    /// </summary>
    public static class ConfigInterpolacaoMundo
    {
        /// <summary>
        /// Quanto a pose mostrada fica atrás do último pacote, em segundos.
        /// Um intervalo de envio (100 ms) permite intercalar entre dois snapshots consecutivos.
        /// </summary>
        public const float AtrasoDeRenderSegundos = 0.10f;

        /// <summary>
        /// Limite de extrapolação (em segundos) seguindo a última velocidade conhecida quando
        /// há atraso ou perda de pacote. Após esse limite, o avatar para e espera.
        /// </summary>
        public const float ExtrapolacaoMaximaSegundos = 0.15f;

        /// <summary>
        /// Deslocamento horizontal (m) entre dois pacotes acima do qual é teletransporte:
        /// a pose salta imediatamente sem deslizar.
        /// </summary>
        public const float DistanciaDeSalto = 8f;

        /// <summary>
        /// Acima desta distância (m) a posição forçada do servidor teleporta o jogador local imediatamente;
        /// abaixo ou igual a isto, a correção local é suavizada.
        /// </summary>
        public const float DistanciaDeCorrecao = 3f;

        /// <summary>
        /// Velocidade (m/s) em que a correção local do jogador avança até a posição forçada quando suavizada.
        /// </summary>
        public const float VelocidadeSuavizacaoCorrecao = 15f;

        /// <summary>
        /// Capacidade máxima do buffer de snapshots em memória.
        /// </summary>
        public const int CapacidadeBuffer = 32;
    }
}
