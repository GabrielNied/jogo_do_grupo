#nullable enable
using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Regras puras e constantes de balanceamento para balões de fala (Speech Bubbles).
    /// </summary>
    public static class RegrasBaloesDeFala
    {
        /// <summary>Distância máxima (em metros) da câmera para renderizar o balão de fala.</summary>
        public const float DistanciaMaximaMetros = 25f;

        /// <summary>Altura padrão (em metros) sobre a base do avatar para ancoragem do balão.</summary>
        public const float AlturaSobreCabecaMetros = 2.4f;

        /// <summary>Duração mínima de exibição do balão de fala (segundos).</summary>
        public const float DuracaoMinimaSegundos = 4.0f;

        /// <summary>Duração máxima de exibição do balão de fala (segundos).</summary>
        public const float DuracaoMaximaSegundos = 7.0f;

        /// <summary>Tempo durante o qual ocorre a transição de fade-out suave (segundos).</summary>
        public const float TempoFadeOutSegundos = 0.8f;

        /// <summary>
        /// Determina se o canal de chat permite a exibição de balão de fala público no mundo 3D.
        /// Apenas Cidade e Grupo são permitidos. Sussurro e Avisos de Sistema são estritamente privados.
        /// </summary>
        public static bool CanalPermiteBalao(CanalDeChat canal)
        {
            return canal == CanalDeChat.Cidade || canal == CanalDeChat.Grupo;
        }

        /// <summary>
        /// Calcula a duração proporcional do balão baseando-se no tamanho do texto limpo (4.0s a 7.0s).
        /// </summary>
        public static float CalcularDuracao(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return DuracaoMinimaSegundos;
            }

            int tamanho = texto.Trim().Length;
            // 4.0s para textos curtos, aumentando proporcionalmente até o teto de 7.0s
            float duracao = 3.5f + tamanho * 0.05f;
            return Math.Clamp(duracao, DuracaoMinimaSegundos, DuracaoMaximaSegundos);
        }

        /// <summary>
        /// Verifica se um balão de fala deve estar visível com base no canal, distância e projeção.
        /// </summary>
        public static bool DeveEstarVisivel(
            CanalDeChat canal,
            float distanciaMetros,
            bool aFrenteDaCamera
        )
        {
            return CanalPermiteBalao(canal)
                && aFrenteDaCamera
                && distanciaMetros >= 0f
                && distanciaMetros <= DistanciaMaximaMetros;
        }
    }
}
