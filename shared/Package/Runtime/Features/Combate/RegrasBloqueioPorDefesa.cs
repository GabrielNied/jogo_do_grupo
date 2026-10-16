using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Regras puras de mitigação e anulação de dano direto por defesa de equipamento (#344).
    /// Se a defesa for pelo menos 1,5x a potência de bloqueio do golpe, o dano direto é anulado (0).
    /// Abaixo disso, há redução proporcional, até o teto de 80% de redução (sem anulação).
    /// </summary>
    public static class RegrasBloqueioPorDefesa
    {
        /// <summary>Multiplicador sobre a potência de bloqueio exigido para anular o golpe direto (1,5x).</summary>
        public const double MultiplicadorDefesaParaAnular = 1.5;

        /// <summary>Redução máxima proporcional quando a defesa não anula completamente (80%).</summary>
        public const double ReducaoMaximaProporcional = 0.80;

        /// <summary>
        /// Determina se a defesa de equipamento anula completamente o golpe.
        /// </summary>
        public static bool AnulaCompletamente(double defesa, double potenciaBloqueio)
        {
            if (potenciaBloqueio <= 0)
            {
                return true;
            }

            return defesa >= MultiplicadorDefesaParaAnular * potenciaBloqueio;
        }

        /// <summary>
        /// Calcula a fração de redução de dano (0.0 a 0.80, ou 1.0 se anula).
        /// </summary>
        public static double FracaoDeReducao(double defesa, double potenciaBloqueio)
        {
            if (potenciaBloqueio <= 0)
            {
                return 1.0;
            }

            if (defesa <= 0)
            {
                return 0.0;
            }

            if (AnulaCompletamente(defesa, potenciaBloqueio))
            {
                return 1.0;
            }

            double proporcao = defesa / (MultiplicadorDefesaParaAnular * potenciaBloqueio);
            if (proporcao > 1.0)
            {
                proporcao = 1.0;
            }

            return proporcao * ReducaoMaximaProporcional;
        }

        /// <summary>
        /// Calcula o dano final recebido após a mitigação da defesa de equipamento.
        /// Se a defesa anula (>= 1,5x potência), devolve 0.
        /// Se reduz proporcionalmente, devolve o dano atenuado, com piso de 1 se danoOriginal > 0 (sem anulação abaixo de 1,5x).
        /// </summary>
        public static int CalcularDanoFinal(
            int danoOriginal,
            double defesa,
            double potenciaBloqueio
        )
        {
            if (danoOriginal <= 0)
            {
                return 0;
            }

            if (AnulaCompletamente(defesa, potenciaBloqueio))
            {
                return 0;
            }

            double reducao = FracaoDeReducao(defesa, potenciaBloqueio);
            double danoAposReducao = danoOriginal * (1.0 - reducao);
            int final = (int)Math.Round(danoAposReducao, MidpointRounding.AwayFromZero);

            return Math.Max(1, final);
        }
    }
}
