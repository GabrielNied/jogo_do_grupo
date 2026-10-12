using System;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>De onde vem o loot: decide o peso das raridades (<see cref="ConfigLoot"/>).</summary>
    public enum OrigemDoLoot
    {
        Comum = 0,
        Fortalecida = 1,
        ChefeRaro = 2,
        Chefe = 3,
    }

    /// <summary>Uma peça que um inimigo deixou cair: modelo do catálogo, raridade e grau.</summary>
    [Serializable]
    public class QuedaDeLoot
    {
        public int ModeloId;
        public RaridadeItem Raridade;
        public GrauEquipamento Grau;
    }

    /// <summary>
    /// A tabela de loot dos inimigos (issue #493): modelo uniforme entre as 72 peças, raridade
    /// pela origem e grau pelo nível do inimigo. Funções puras; o sorteio de verdade (e a
    /// quantidade por participante) é do servidor. Valores em <see cref="ConfigLoot"/>.
    /// </summary>
    public static class TabelaDeLoot
    {
        /// <summary>Origem do loot a partir da ficha do inimigo e da variante com que apareceu.</summary>
        public static OrigemDoLoot OrigemDe(
            CategoriaDeCriatura categoria,
            VarianteDeCriatura variante
        )
        {
            if (categoria == CategoriaDeCriatura.Chefe)
            {
                return OrigemDoLoot.Chefe;
            }

            if (categoria == CategoriaDeCriatura.ChefeRaro)
            {
                return OrigemDoLoot.ChefeRaro;
            }

            return variante == VarianteDeCriatura.Fortalecida
                ? OrigemDoLoot.Fortalecida
                : OrigemDoLoot.Comum;
        }

        /// <summary>Grau da peça pelo nível do inimigo (nível menor que 1 conta como 1).</summary>
        public static GrauEquipamento GrauDoNivel(int nivelDoInimigo)
        {
            if (nivelDoInimigo >= ConfigLoot.NivelDoInimigoParaGrauForte)
            {
                return GrauEquipamento.Forte;
            }

            return nivelDoInimigo >= ConfigLoot.NivelDoInimigoParaGrauMedio
                ? GrauEquipamento.Medio
                : GrauEquipamento.Inicial;
        }

        /// <summary>Nível de personagem exigido para equipar uma peça do grau: 1, 10 ou 20.</summary>
        public static int NivelMinimoDoGrau(GrauEquipamento grau)
        {
            if (grau == GrauEquipamento.Forte)
            {
                return ConfigLoot.NivelDoInimigoParaGrauForte;
            }

            return grau == GrauEquipamento.Medio ? ConfigLoot.NivelDoInimigoParaGrauMedio : 1;
        }

        /// <summary>Pesos de raridade da origem, na ordem de <see cref="RaridadeItem"/>.</summary>
        public static int[] PesosDe(OrigemDoLoot origem)
        {
            switch (origem)
            {
                case OrigemDoLoot.Fortalecida:
                    return ConfigLoot.PesosDoInimigoFortalecido;
                case OrigemDoLoot.ChefeRaro:
                    return ConfigLoot.PesosDoChefeRaro;
                case OrigemDoLoot.Chefe:
                    return ConfigLoot.PesosDoChefe;
                default:
                    return ConfigLoot.PesosDoInimigoComum;
            }
        }

        /// <summary>
        /// Raridade para um número sorteado em [0, 1) (fora da faixa vale a ponta mais próxima).
        /// </summary>
        public static RaridadeItem RaridadeSorteada(OrigemDoLoot origem, double sorteio)
        {
            var pesos = PesosDe(origem);
            double total = 0;
            for (var i = 0; i < pesos.Length; i++)
            {
                total += pesos[i];
            }

            var alvo = Math.Min(Math.Max(sorteio, 0.0), 0.999999999) * total;
            double acumulado = 0;
            for (var i = 0; i < pesos.Length; i++)
            {
                acumulado += pesos[i];
                if (pesos[i] > 0 && alvo < acumulado)
                {
                    return (RaridadeItem)i;
                }
            }

            return RaridadeItem.Comum;
        }

        /// <summary>
        /// Sorteia uma peça: modelo uniforme entre os 72 do catálogo, raridade pela origem, grau
        /// pelo nível do inimigo. Cada chamada gasta três números de <paramref name="aleatorio"/>.
        /// </summary>
        public static QuedaDeLoot Sortear(Random aleatorio, OrigemDoLoot origem, int nivelDoInimigo)
        {
            var modelo =
                CatalogoEquipamentos.PrimeiroModelo
                + aleatorio.Next(CatalogoEquipamentos.QuantidadeModelos);
            return new QuedaDeLoot
            {
                ModeloId = modelo,
                Raridade = RaridadeSorteada(origem, aleatorio.NextDouble()),
                Grau = GrauDoNivel(nivelDoInimigo),
            };
        }

        /// <summary>
        /// Pontuação para comparar peças: soma dos atributos (o grau já está neles) mais
        /// <see cref="ConfigLoot.BonusDePontuacaoPorRaridade"/> por degrau de raridade.
        /// </summary>
        public static double Pontuacao(Equipamento peca)
        {
            if (peca == null)
            {
                return 0;
            }

            double soma = 0;
            for (var i = 0; i < peca.Atributos.Length; i++)
            {
                soma += peca.Atributos[i].Valor;
            }

            return soma * (1 + ConfigLoot.BonusDePontuacaoPorRaridade * (int)peca.Raridade);
        }
    }
}
