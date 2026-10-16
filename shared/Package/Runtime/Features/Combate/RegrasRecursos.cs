using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fórmulas puras de atributos e recursos (vida, stamina, mana). Valores da proposta em
    /// docs/features/fatia-jogavel.md, seção 1; o servidor é quem decide, o client só mostra.
    /// </summary>
    public static class RegrasRecursos
    {
        // Números propostos pela simulação da #627 (docs/features/personagem.md, "Atributos
        // do personagem"); o PR da #627 é a aprovação.
        public const int VidaBase = 500;

        /// <summary>Vida máxima ganha por ponto de Vigor.</summary>
        public const int VidaPorVigor = 12;
        public const int StaminaBase = 100;

        /// <summary>Stamina máxima ganha por ponto de Agilidade.</summary>
        public const int StaminaPorAgilidade = 2;
        public const int ManaBase = 100;

        /// <summary>Mana máxima ganha por ponto de Espírito.</summary>
        public const int ManaPorEspirito = 2;

        /// <summary>Segundos sem gastar o recurso antes de a regeneração começar.</summary>
        public const double AtrasoRegeneracaoSegundos = 2.0;

        public const double RegeneracaoStaminaPorSegundo = 10.0;
        public const double RegeneracaoManaPorSegundo = 5.0;

        /// <summary>
        /// Valor final de um atributo do personagem: base + pontos distribuídos + efeitos. O
        /// equipamento não entra (DEC-001): os atributos dos itens são uma fonte à parte.
        /// </summary>
        public static int ValorFinal(int basePersonagem, int pontosDistribuidos, int efeitos) =>
            basePersonagem + pontosDistribuidos + efeitos;

        public static int VidaMaxima(int vigor) => VidaBase + VidaPorVigor * vigor;

        /// <summary>Vida máxima com o que o Vigor dos itens dá direto (já convertido).</summary>
        public static int VidaMaxima(int vigor, int deEquipamentos) =>
            VidaMaxima(vigor) + deEquipamentos;

        public static int StaminaMaxima(int agilidade) =>
            StaminaBase + StaminaPorAgilidade * agilidade;

        /// <summary>Stamina máxima com o que a Agilidade dos itens dá direto (já convertida).</summary>
        public static int StaminaMaxima(int agilidade, int deEquipamentos) =>
            StaminaMaxima(agilidade) + deEquipamentos;

        public static int ManaMaxima(int espirito) => ManaBase + ManaPorEspirito * espirito;

        /// <summary>Mana máxima com o que o Espírito dos itens dá direto (já convertido).</summary>
        public static int ManaMaxima(int espirito, int deEquipamentos) =>
            ManaMaxima(espirito) + deEquipamentos;

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
        /// Regeneração de stamina: devolve o novo atual. Só conta o tempo que passou depois do
        /// atraso de <see cref="AtrasoRegeneracaoSegundos"/> desde o último gasto.
        /// </summary>
        public static int RegenerarStamina(
            int atual,
            int maximo,
            double segundosDesdeUltimoGasto,
            double segundosPassados
        ) =>
            Regenerar(
                atual,
                maximo,
                RegeneracaoStaminaPorSegundo,
                segundosDesdeUltimoGasto,
                segundosPassados
            );

        /// <summary>Regeneração de mana (mesma regra da stamina, outra taxa).</summary>
        public static int RegenerarMana(
            int atual,
            int maximo,
            double segundosDesdeUltimoGasto,
            double segundosPassados
        ) =>
            Regenerar(
                atual,
                maximo,
                RegeneracaoManaPorSegundo,
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
