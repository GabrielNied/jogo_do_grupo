using System;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>Os sons do preparo de skill forte.</summary>
    internal enum SomDeSkill
    {
        /// <summary>Tom que sobe durante o preparo.</summary>
        Preparo,

        /// <summary>Estalo curto e grave de quando o preparo é cancelado.</summary>
        Cancelado,
    }

    /// <summary>
    /// Gera os sons do preparo como ondas simples, sem arquivos de terceiros (mesmo caminho do
    /// <c>SintetizadorDeSons</c> de Combate; Skills não referencia Combate). Puro e determinístico.
    /// </summary>
    internal static class SonsDeSkills
    {
        public const int TaxaDeAmostragem = 22050;

        public static float[] Gerar(SomDeSkill som, float segundosDoPreparo)
        {
            switch (som)
            {
                case SomDeSkill.Preparo:
                    return Tom(Math.Max(0.2f, segundosDoPreparo), 220f, 660f, 0.35f, true);
                case SomDeSkill.Cancelado:
                    return Tom(0.18f, 330f, 110f, 0.4f, false);
                default:
                    return Array.Empty<float>();
            }
        }

        // Tom que vai de <de> a <ate> Hz. Crescente: o volume sobe até o fim (energia acumulando);
        // senão decai (estalo).
        private static float[] Tom(
            float segundos,
            float de,
            float ate,
            float volume,
            bool crescente
        )
        {
            var total = (int)(segundos * TaxaDeAmostragem);
            var amostras = new float[total];
            var fase = 0.0;
            for (var i = 0; i < total; i++)
            {
                var t = i / (float)total;
                var frequencia = de + (ate - de) * t;
                fase += 2.0 * Math.PI * frequencia / TaxaDeAmostragem;
                var envelope = crescente ? 0.25f + 0.75f * t : (1f - t) * (1f - t);
                // Fecha com rampa curta para não estalar no fim.
                var rampa = Math.Min(1f, (1f - t) * total / 220f);
                amostras[i] = (float)Math.Sin(fase) * volume * envelope * rampa;
            }

            return amostras;
        }
    }
}
