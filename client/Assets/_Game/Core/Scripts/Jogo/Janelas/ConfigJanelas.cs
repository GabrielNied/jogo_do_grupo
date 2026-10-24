using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>Sensação de mexer nas janelas do jogo (ajustável no F10, grupo Janelas).</summary>
    public static class ConfigJanelas
    {
        /// <summary>
        /// Quanto o mouse anda (pixels da interface) com o botão apertado no cabeçalho antes de a
        /// janela começar a ser arrastada; abaixo disso o clique só traz a janela para cima.
        /// </summary>
        public static float LimiarDeArrasto = 4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            Sintonia.Registrar(
                "Janelas",
                nameof(LimiarDeArrasto),
                () => LimiarDeArrasto,
                v => LimiarDeArrasto = v,
                0f,
                24f,
                "px que o mouse anda no cabeçalho antes de arrastar a janela"
            );
        }
    }
}
