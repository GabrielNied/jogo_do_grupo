using System;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Fórmulas puras de criaturas: a variante elite e a aparição do chefe raro. Valores
    /// da proposta em docs/features/mobs.md; o servidor é quem sorteia e decide, o client só
    /// mostra. Os sorteios recebem um número de 0 (inclusive) a 1 (exclusive).
    /// </summary>
    public static class RegrasCriaturas
    {
        /// <summary>Chance, de 0 a 1, de uma criatura comum aparecer elite.</summary>
        public const double ChanceDeElite = 0.05;

        public const double MultiplicadorDeVidaElite = 2.0;
        public const double MultiplicadorDeDanoElite = 1.5;

        /// <summary>Multiplica a experiência e a quantidade de sorteios de loot.</summary>
        public const double MultiplicadorDeRecompensaElite = 2.0;

        /// <summary>Tamanho do modelo em relação ao normal (1 = igual).</summary>
        public const float EscalaElite = 1.3f;

        /// <summary>
        /// Tamanho do modelo do chefe de mundo (proposta, a calibrar): ele reaproveita o modelo de
        /// outra criatura, só maior.
        /// </summary>
        public const float EscalaDoChefeDeMundo = 2.5f;

        /// <summary>Só a criatura comum pode vir elite; chefes têm força fixa.</summary>
        public static bool PodeSerElite(CategoriaDeCriatura categoria) =>
            categoria == CategoriaDeCriatura.Comum;

        /// <summary>
        /// Chefe raro e chefe de mundo avisam na área toda quando aparecem e ficam marcados no
        /// mapa e no minimapa, mesmo longe (o raio de interesse dos comuns não vale para eles).
        /// </summary>
        public static bool AvisaNaArea(CategoriaDeCriatura categoria) =>
            categoria == CategoriaDeCriatura.ChefeRaro
            || categoria == CategoriaDeCriatura.ChefeDeMundo;

        /// <summary>Variante de uma criatura que acabou de aparecer.</summary>
        public static VarianteDeCriatura SortearVariante(
            CategoriaDeCriatura categoria,
            double sorteio
        )
        {
            if (PodeSerElite(categoria) && sorteio < ChanceDeElite)
            {
                return VarianteDeCriatura.Elite;
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
            Multiplicar(vidaBase, variante, MultiplicadorDeVidaElite);

        public static int Dano(int danoBase, VarianteDeCriatura variante) =>
            Multiplicar(danoBase, variante, MultiplicadorDeDanoElite);

        public static int Experiencia(int experienciaBase, VarianteDeCriatura variante) =>
            Multiplicar(experienciaBase, variante, MultiplicadorDeRecompensaElite);

        /// <summary>Quanto a recompensa (experiência e loot) cresce: 1 para a normal.</summary>
        public static double MultiplicadorDeRecompensa(VarianteDeCriatura variante)
        {
            if (variante == VarianteDeCriatura.Elite)
            {
                return MultiplicadorDeRecompensaElite;
            }
            return 1.0;
        }

        /// <summary>Tamanho do modelo: 1 para a normal.</summary>
        public static float Escala(VarianteDeCriatura variante)
        {
            if (variante == VarianteDeCriatura.Elite)
            {
                return EscalaElite;
            }
            return 1f;
        }

        private static int Multiplicar(
            int valorBase,
            VarianteDeCriatura variante,
            double multiplicadorElite
        )
        {
            if (variante != VarianteDeCriatura.Elite)
            {
                return valorBase;
            }
            double valor = Math.Round(
                valorBase * multiplicadorElite,
                MidpointRounding.AwayFromZero
            );
            return (int)Math.Min(valor, int.MaxValue);
        }
    }
}
