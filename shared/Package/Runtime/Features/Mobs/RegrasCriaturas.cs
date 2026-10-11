using System;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Fórmulas puras de criaturas: a variante fortalecida e a aparição do chefe raro. Valores
    /// da proposta em docs/features/mobs.md; o servidor é quem sorteia e decide, o client só
    /// mostra. Os sorteios recebem um número de 0 (inclusive) a 1 (exclusive).
    /// </summary>
    public static class RegrasCriaturas
    {
        /// <summary>Chance, de 0 a 1, de uma criatura comum aparecer fortalecida.</summary>
        public const double ChanceDeFortalecida = 0.05;

        public const double MultiplicadorDeVidaFortalecida = 2.0;
        public const double MultiplicadorDeDanoFortalecida = 1.5;

        /// <summary>Multiplica a experiência e a quantidade de sorteios de loot.</summary>
        public const double MultiplicadorDeRecompensaFortalecida = 2.0;

        /// <summary>Tamanho do modelo em relação ao normal (1 = igual).</summary>
        public const float EscalaFortalecida = 1.3f;

        /// <summary>Só a criatura comum pode vir fortalecida; chefes têm força fixa.</summary>
        public static bool PodeSerFortalecida(CategoriaDeCriatura categoria) =>
            categoria == CategoriaDeCriatura.Comum;

        /// <summary>Variante de uma criatura que acabou de aparecer.</summary>
        public static VarianteDeCriatura SortearVariante(
            CategoriaDeCriatura categoria,
            double sorteio
        )
        {
            if (PodeSerFortalecida(categoria) && sorteio < ChanceDeFortalecida)
            {
                return VarianteDeCriatura.Fortalecida;
            }
            return VarianteDeCriatura.Normal;
        }

        /// <summary>
        /// Se a criatura aparece neste sorteio. Chance 1 (ou mais) sempre aparece; chance 0 (ou
        /// menos) nunca.
        /// </summary>
        public static bool Aparece(double chanceDeAparecer, double sorteio) =>
            sorteio < chanceDeAparecer;

        public static int VidaMaxima(int vidaBase, VarianteDeCriatura variante) =>
            Multiplicar(vidaBase, variante, MultiplicadorDeVidaFortalecida);

        public static int Dano(int danoBase, VarianteDeCriatura variante) =>
            Multiplicar(danoBase, variante, MultiplicadorDeDanoFortalecida);

        public static int Experiencia(int experienciaBase, VarianteDeCriatura variante) =>
            Multiplicar(experienciaBase, variante, MultiplicadorDeRecompensaFortalecida);

        /// <summary>Quanto a recompensa (experiência e loot) cresce: 1 para a normal.</summary>
        public static double MultiplicadorDeRecompensa(VarianteDeCriatura variante)
        {
            if (variante == VarianteDeCriatura.Fortalecida)
            {
                return MultiplicadorDeRecompensaFortalecida;
            }
            return 1.0;
        }

        /// <summary>Tamanho do modelo: 1 para a normal.</summary>
        public static float Escala(VarianteDeCriatura variante)
        {
            if (variante == VarianteDeCriatura.Fortalecida)
            {
                return EscalaFortalecida;
            }
            return 1f;
        }

        private static int Multiplicar(
            int valorBase,
            VarianteDeCriatura variante,
            double multiplicadorFortalecida
        )
        {
            if (variante != VarianteDeCriatura.Fortalecida)
            {
                return valorBase;
            }
            double valor = Math.Round(
                valorBase * multiplicadorFortalecida,
                MidpointRounding.AwayFromZero
            );
            return (int)Math.Min(valor, int.MaxValue);
        }
    }
}
