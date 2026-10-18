using System;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>De onde vem o loot: decide quantas rolagens cada atributo ganha (<see cref="ConfigLoot"/>).</summary>
    public enum OrigemDoLoot
    {
        Comum = 0,
        Elite = 1,
        ChefeRaro = 2,
        Chefe = 3,
    }

    /// <summary>
    /// Uma peça que um inimigo deixou cair: modelo do catálogo, origem e grau. Os valores dos
    /// atributos (e com eles o tier) só são sorteados quando a peça entra no inventário.
    /// </summary>
    [Serializable]
    public class QuedaDeLoot
    {
        public int ModeloId;
        public OrigemDoLoot Origem;
        public GrauEquipamento Grau;
    }

    /// <summary>
    /// A tabela de loot dos inimigos (issues #493 e #615): modelo uniforme entre as 72 peças, grau
    /// pelo nível do inimigo e origem guardada para melhorar o sorteio dos valores dos atributos. Funções puras; o sorteio de verdade (e a
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

            return variante == VarianteDeCriatura.Elite ? OrigemDoLoot.Elite : OrigemDoLoot.Comum;
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

        /// <summary>
        /// Quantas rolagens cada atributo ganha na origem: o valor é o melhor delas
        /// (<see cref="RegrasTier.SortearValor"/>).
        /// </summary>
        public static int RolagensDe(OrigemDoLoot origem)
        {
            switch (origem)
            {
                case OrigemDoLoot.Elite:
                    return ConfigLoot.RolagensDoInimigoElite;
                case OrigemDoLoot.ChefeRaro:
                    return ConfigLoot.RolagensDoChefeRaro;
                case OrigemDoLoot.Chefe:
                    return ConfigLoot.RolagensDoChefe;
                default:
                    return ConfigLoot.RolagensDoInimigoComum;
            }
        }

        /// <summary>Modelo de equipamento exclusivo que só o chefe raro larga.</summary>
        public const int ModeloExclusivoDoChefeRaro = CatalogoEquipamentos.AdornoPresaVelha;

        /// <summary>Exemplo de item de pet (cervo); o sorteio real usa <see cref="RegrasLootCompanheiros"/>.</summary>
        public const int ItemPetRaro = CatalogoItens.SinoDoCervoJovem;

        /// <summary>Exemplo de montaria de chefe; o sorteio real usa <see cref="RegrasLootCompanheiros"/>.</summary>
        public const int ItemMontariaChefe = CatalogoItens.RedeasDoLoboDePresa;

        /// <summary>
        /// Sorteia uma peça: modelo uniforme entre os 72 do catálogo (ou o modelo exclusivo se chefe raro),
        /// grau pelo nível do inimigo e a origem guardada. Cada chamada gasta números de <paramref name="aleatorio"/>.
        /// </summary>
        public static QuedaDeLoot Sortear(Random aleatorio, OrigemDoLoot origem, int nivelDoInimigo)
        {
            int modelo;
            if (
                origem == OrigemDoLoot.ChefeRaro
                && aleatorio.NextDouble() < ConfigLoot.ChanceDeItemExclusivoDoChefeRaro
            )
            {
                modelo = ModeloExclusivoDoChefeRaro;
            }
            else
            {
                modelo =
                    CatalogoEquipamentos.PrimeiroModelo
                    + aleatorio.Next(CatalogoEquipamentos.QuantidadeModelos);
            }

            return new QuedaDeLoot
            {
                ModeloId = modelo,
                Origem = origem,
                Grau = GrauDoNivel(nivelDoInimigo),
            };
        }

        /// <summary>
        /// Pontuação para comparar peças: soma dos valores dos atributos mais
        /// <see cref="ConfigLoot.BonusDePontuacaoPorTier"/> por degrau do tier atual.
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

            return soma * (1 + ConfigLoot.BonusDePontuacaoPorTier * (int)RegrasTier.TierDe(peca));
        }
    }
}
