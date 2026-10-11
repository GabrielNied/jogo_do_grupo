using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fórmulas puras de atributos e recursos (vida, vigor, energia). Valores da proposta em
    /// docs/features/fatia-jogavel.md, seção 1; o servidor é quem decide, o client só mostra.
    /// </summary>
    public static class RegrasRecursos
    {
        public const int VidaBase = 500;
        public const int VidaPorVitalidade = 10;
        public const int VigorBase = 100;
        public const int EnergiaBase = 100;
        public const int EnergiaPorEspirito = 2;

        /// <summary>Segundos sem gastar o recurso antes de a regeneração começar.</summary>
        public const double AtrasoRegeneracaoSegundos = 2.0;

        public const double RegeneracaoVigorPorSegundo = 10.0;
        public const double RegeneracaoEnergiaPorSegundo = 5.0;

        /// <summary>Valor final de um atributo: base + pontos distribuídos + equipamentos + efeitos.</summary>
        public static int ValorFinal(
            int basePersonagem,
            int pontosDistribuidos,
            int equipamentos,
            int efeitos
        ) => basePersonagem + pontosDistribuidos + equipamentos + efeitos;

        public static int VidaMaxima(int vitalidade) => VidaBase + VidaPorVitalidade * vitalidade;

        public static int VigorMaximo() => VigorBase;

        public static int EnergiaMaxima(int espirito) =>
            EnergiaBase + EnergiaPorEspirito * espirito;

        /// <summary>
        /// Mantém o valor atual entre 0 e o máximo. Reduzir o máximo não cura: o atual só
        /// diminui se passar do novo máximo.
        /// </summary>
        public static int Limitar(int atual, int maximo)
        {
            if (maximo < 0)
            {
                maximo = 0;
            }
            return Math.Max(0, Math.Min(atual, maximo));
        }

        /// <summary>
        /// Regeneração de vigor: devolve o novo atual. Só conta o tempo que passou depois do
        /// atraso de <see cref="AtrasoRegeneracaoSegundos"/> desde o último gasto.
        /// </summary>
        public static int RegenerarVigor(
            int atual,
            int maximo,
            double segundosDesdeUltimoGasto,
            double segundosPassados
        ) =>
            Regenerar(
                atual,
                maximo,
                RegeneracaoVigorPorSegundo,
                segundosDesdeUltimoGasto,
                segundosPassados
            );

        /// <summary>Regeneração de energia (mesma regra do vigor, outra taxa).</summary>
        public static int RegenerarEnergia(
            int atual,
            int maximo,
            double segundosDesdeUltimoGasto,
            double segundosPassados
        ) =>
            Regenerar(
                atual,
                maximo,
                RegeneracaoEnergiaPorSegundo,
                segundosDesdeUltimoGasto,
                segundosPassados
            );

        /// <summary>
        /// Regeneração genérica. <paramref name="segundosDesdeUltimoGasto"/> é o tempo desde o
        /// último gasto no <b>fim</b> do intervalo; <paramref name="segundosPassados"/> é a
        /// duração do intervalo. O ganho é arredondado para baixo (inteiro).
        /// </summary>
        public static int Regenerar(
            int atual,
            int maximo,
            double taxaPorSegundo,
            double segundosDesdeUltimoGasto,
            double segundosPassados
        )
        {
            int limitado = Limitar(atual, maximo);
            double efetivo = Math.Min(
                segundosPassados,
                segundosDesdeUltimoGasto - AtrasoRegeneracaoSegundos
            );
            if (efetivo <= 0 || taxaPorSegundo <= 0)
            {
                return limitado;
            }
            double novo = limitado + Math.Floor(efetivo * taxaPorSegundo);
            return Limitar((int)Math.Min(novo, int.MaxValue), maximo);
        }
    }
}
