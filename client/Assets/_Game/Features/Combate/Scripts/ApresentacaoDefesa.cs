using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Os números da defesa como o client os usa (a regra e o custo são do servidor).</summary>
    internal static class ApresentacaoDefesa
    {
        public const string TextoDefesaInsuficientePadrao =
            "Defesa insuficiente para bloquear completamente este impacto.";

        /// <summary>A fração da corrida permitida com a guarda segurada (0,4).</summary>
        public static float FatorDaGuarda => (float)RegrasGuarda.VelocidadeComGuarda;

        /// <summary>
        /// Monta o texto de aviso quando a defesa absorve apenas parte do dano.
        /// Mostra quanto dano residual passou em relação ao dano original (ex.: "12 de 40 passaram").
        /// Se os números não forem válidos (original <= 0 ou residual <= 0), recorre ao texto padrão.
        /// </summary>
        public static string TextoDefesaInsuficiente(int danoResidual, int danoOriginal)
        {
            if (danoOriginal > 0 && danoResidual > 0)
            {
                return danoResidual == 1
                    ? $"1 de {danoOriginal} passou"
                    : $"{danoResidual} de {danoOriginal} passaram";
            }

            return TextoDefesaInsuficientePadrao;
        }
    }
}
