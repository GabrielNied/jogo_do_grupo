using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// O tier calculado de um exemplar: qualidade média (0 a 100) dos atributos elegíveis e o
    /// tier que ela dá.
    /// </summary>
    public readonly struct ResultadoTier
    {
        public readonly TierItem Tier;

        /// <summary>Média das qualidades dos atributos elegíveis, de 0 a 100.</summary>
        public readonly double Qualidade;

        /// <summary>Quantos atributos do exemplar entraram na conta.</summary>
        public readonly int Elegiveis;

        public ResultadoTier(TierItem tier, double qualidade, int elegiveis)
        {
            Tier = tier;
            Qualidade = qualidade;
            Elegiveis = elegiveis;
        }
    }

    /// <summary>
    /// O tier de um equipamento (issue #615, TIER-001 a TIER-008). Funções puras e genéricas sobre
    /// <see cref="StatusItem"/>: nenhuma regra cita um atributo, tudo vem de <see cref="ConfigTier"/>
    /// (faixas, elegíveis e limiares). A fusão (#616) usa <see cref="Calcular(Equipamento)"/> para
    /// recalcular o tier depois de mudar um atributo e <see cref="ConfigTier.Maximo"/> para o teto.
    /// </summary>
    public static class RegrasTier
    {
        /// <summary>
        /// Versão da escala dos atributos gravada no exemplar (<see cref="Equipamento.VersaoDosAtributos"/>):
        /// 0 é o legado (escala do catálogo, fracionária); 1 é a escala inteira de 0 a 100 com
        /// <see cref="Equipamento.TierOrigem"/> gravado (#615); 2 são os atributos próprios dos
        /// equipamentos (#623, <see cref="MigracaoDeAtributos"/>).
        /// </summary>
        /// <summary>Stats de combate diretos (dano físico/mágico, vida); sem Força/Vigor/Agilidade/Intelecto/Espírito do item (#709).</summary>
        public const int VersaoAtual = 3;

        /// <summary>O tier atual do exemplar: só os valores dos atributos mandam (TIER-002).</summary>
        public static TierItem TierDe(Equipamento item) => Calcular(item).Tier;

        public static ResultadoTier Calcular(Equipamento item) => Calcular(item, ConfigTier.Faixas);

        /// <summary>
        /// Calcula o tier com uma tabela de faixas dada. Só entram os atributos do exemplar que são
        /// elegíveis na categoria dele; o que o exemplar não tem não conta como zero (TIER-007).
        /// Sem nenhum atributo elegível a qualidade é 0 (tier D).
        /// </summary>
        public static ResultadoTier Calcular(Equipamento item, TabelaDeFaixas faixas)
        {
            if (item == null || item.Atributos == null)
            {
                return new ResultadoTier(TierItem.D, 0, 0);
            }

            double soma = 0;
            var contados = 0;
            var todosNoMaximo = true;
            for (var i = 0; i < item.Atributos.Length; i++)
            {
                var atributo = item.Atributos[i];
                var faixa = faixas.Faixa(item.Tipo, atributo.Status);
                if (faixa == null)
                {
                    continue;
                }

                soma += Qualidade(atributo.Valor, faixa.Value);
                contados++;
                if (atributo.Valor < faixa.Value.Maximo)
                {
                    todosNoMaximo = false;
                }
            }

            if (contados == 0)
            {
                return new ResultadoTier(TierItem.D, 0, 0);
            }

            var media = soma / contados;
            var tier = TierDaQualidade(media);
            if (tier == TierItem.S && todosNoMaximo)
            {
                // TIER-005: S+ é condição do estado do item, não um tier sorteado.
                tier = TierItem.SMais;
            }

            return new ResultadoTier(tier, media, contados);
        }

        /// <summary>
        /// Qualidade de um atributo (TIER-008): <c>(V - Vmin) / (Vmax - Vmin) x 100</c>, de 0 a 100.
        /// Faixa sem amplitude vale 100 quando o valor chega ao máximo.
        /// </summary>
        public static double Qualidade(double valor, FaixaDeAtributo faixa)
        {
            if (faixa.Amplitude <= 0)
            {
                return valor >= faixa.Maximo ? 100.0 : 0.0;
            }

            var qualidade = (valor - faixa.Minimo) / faixa.Amplitude * 100.0;
            return Math.Min(Math.Max(qualidade, 0.0), 100.0);
        }

        /// <summary>O tier de uma qualidade média, sem a condição S+ (que olha os atributos).</summary>
        public static TierItem TierDaQualidade(double qualidade)
        {
            var q = qualidade + ConfigTier.Tolerancia;
            if (q >= ConfigTier.LimiarS)
            {
                return TierItem.S;
            }

            if (q >= ConfigTier.LimiarA)
            {
                return TierItem.A;
            }

            if (q >= ConfigTier.LimiarB)
            {
                return TierItem.B;
            }

            return q >= ConfigTier.LimiarC ? TierItem.C : TierItem.D;
        }

        /// <summary>A letra do tier: "D", "C", "B", "A", "S" ou "S+".</summary>
        public static string Letra(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.C:
                    return "C";
                case TierItem.B:
                    return "B";
                case TierItem.A:
                    return "A";
                case TierItem.S:
                    return "S";
                case TierItem.SMais:
                    return "S+";
                default:
                    return "D";
            }
        }

        /// <summary>
        /// Sorteia o valor de um atributo na faixa: o grau escolhe a janela da faixa e o valor é o
        /// melhor de <paramref name="rolagens"/> sorteios dentro dela (a origem do inimigo sobe as
        /// rolagens). Cada rolagem gasta um número de <paramref name="aleatorio"/>.
        /// </summary>
        public static int SortearValor(
            Random aleatorio,
            FaixaDeAtributo faixa,
            GrauEquipamento grau,
            int rolagens
        )
        {
            var inicio = faixa.Minimo + faixa.Amplitude * ConfigTier.InicioDoGrau(grau);
            var fim = faixa.Minimo + faixa.Amplitude * ConfigTier.FimDoGrau(grau);
            var melhor = double.MinValue;
            for (var i = 0; i < Math.Max(rolagens, 1); i++)
            {
                melhor = Math.Max(melhor, inicio + (fim - inicio) * aleatorio.NextDouble());
            }

            return faixa.Limitar(melhor);
        }
    }
}
