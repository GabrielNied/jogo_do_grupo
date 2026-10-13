using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Valores ajustáveis da fusão (issue #616, FUSAO-009). São os valores iniciais de teste do
    /// Gabriel (2026-10-12), não balanceamento final; as contas ficam em
    /// <see cref="RegrasFusao"/>. Os vetores são indexados pelo nível <b>atual</b> do principal
    /// (0 é o custo de +0 para +1).
    /// </summary>
    public static class ConfigFusao
    {
        /// <summary>Nível máximo de fusão (FUSAO-007).</summary>
        public const int NivelMaximo = 10;

        /// <summary>Coroas de cada fusão, de +0 → +1 até +9 → +10. Total 15.500.</summary>
        private static readonly long[] CoroasPorNivel =
        {
            100,
            200,
            350,
            550,
            800,
            1200,
            1700,
            2400,
            3400,
            4800,
        };

        /// <summary>
        /// Materiais de dungeon de cada fusão: 1, 2 e 3 de +5 → +6 a +7 → +8. Os materiais são a
        /// #617: até lá o servidor recusa a fusão que exige algum.
        /// </summary>
        private static readonly int[] MateriaisDungeonPorNivel = { 0, 0, 0, 0, 0, 1, 2, 3, 0, 0 };

        /// <summary>Materiais de raid de cada fusão: 1 em +8 → +9 e 2 em +9 → +10.</summary>
        private static readonly int[] MateriaisRaidPorNivel = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2 };

        /// <summary>Coroas para subir do <paramref name="nivelAtual"/> para o próximo.</summary>
        public static long Custo(int nivelAtual) => CoroasPorNivel[Indice(nivelAtual)];

        /// <summary>Materiais de dungeon que a fusão consome.</summary>
        public static int MateriaisDungeon(int nivelAtual) =>
            MateriaisDungeonPorNivel[Indice(nivelAtual)];

        /// <summary>Materiais de raid que a fusão consome.</summary>
        public static int MateriaisRaid(int nivelAtual) =>
            MateriaisRaidPorNivel[Indice(nivelAtual)];

        /// <summary>Coroas de +0 a +10.</summary>
        public static long CustoTotal()
        {
            long total = 0;
            foreach (var custo in CoroasPorNivel)
                total += custo;
            return total;
        }

        private static int Indice(int nivelAtual)
        {
            if (nivelAtual < 0 || nivelAtual >= NivelMaximo)
                throw new ArgumentOutOfRangeException(nameof(nivelAtual));
            return nivelAtual;
        }
    }
}
