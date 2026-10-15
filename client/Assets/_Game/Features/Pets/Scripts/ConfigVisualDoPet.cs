namespace JogoDoGrupo.Client.Features.Pets
{
    /// <summary>
    /// Valores ajustáveis de como o pet aparece (regra 17). O servidor manda a posição a cerca de
    /// 10 Hz; o client suaviza entre os pacotes e dá ao modelo um balanço de passo, já que o
    /// cervo não tem esqueleto.
    /// </summary>
    internal static class ConfigVisualDoPet
    {
        /// <summary>Tempo de suavização da posição (segundos): quanto maior, mais macio e mais atrasado.</summary>
        public const float SuavizacaoDaPosicaoSegundos = 0.12f;

        /// <summary>Velocidade máxima de giro do corpo, em graus por segundo.</summary>
        public const float GiroMaximoGrausPorSegundo = 540f;

        /// <summary>Deslocamento (m) acima do qual o pet pulou de lugar (alcançou o dono): vai direto, sem deslizar.</summary>
        public const float SaltoDeTeletransporteMetros = 4f;

        /// <summary>Passos por segundo do balanço enquanto anda.</summary>
        public const float PassosPorSegundo = 5f;

        /// <summary>Altura do saltinho de cada passo, em metros.</summary>
        public const float AlturaDoPassoMetros = 0.07f;

        /// <summary>Inclinação do corpo para frente e para trás no passo, em graus.</summary>
        public const float BalancoDoPassoGraus = 3f;
    }
}
