using System;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Valores ajustáveis da redistribuição de talentos e dos perfis de build (#627, #910).
    /// </summary>
    public static class ConfigRedistribuicao
    {
        /// <summary>Parte fixa do custo em coroas, paga em qualquer nível.</summary>
        public const long CustoBaseCoroas = 50L;

        /// <summary>
        /// Coroas por nível ao quadrado. Com a base, o custo é 100 no nível 10, 300 no 20, 1.300
        /// no 50 e 5.050 no 100 (a escala da fusão, que custa 15.500 até o +10).
        /// </summary>
        public const double CoroasPorNivelAoQuadrado = 0.5;

        /// <summary>Múltiplo em que o custo é arredondado, para o número ficar redondo.</summary>
        public const long ArredondamentoCoroas = 10L;

        /// <summary>
        /// Custo em coroas de redistribuir todos os pontos da árvore no <paramref name="nivel"/>
        /// do personagem (limitado a 1 e ao nível máximo). Sobe com o quadrado do nível: quem
        /// ainda testa builds paga pouco, quem fechou uma build no fim paga caro.
        /// </summary>
        public static long CustoEmCoroas(int nivel)
        {
            var limitado = Math.Max(1, Math.Min(nivel, ConfigExperiencia.NivelMaximo));
            var bruto = CustoBaseCoroas + CoroasPorNivelAoQuadrado * limitado * limitado;
            return (long)(Math.Round(bruto / ArredondamentoCoroas) * ArredondamentoCoroas);
        }

        /// <summary>Limite máximo de perfis de build salvos por personagem.</summary>
        public const int LimitePerfis = 5;

        /// <summary>Tamanho máximo permitido para o nome de um perfil de build.</summary>
        public const int TamanhoMaximoNomePerfil = 32;
    }
}
