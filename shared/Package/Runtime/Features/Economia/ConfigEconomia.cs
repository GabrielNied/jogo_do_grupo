using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Constantes e regras calibráveis da economia (preços de venda, catálogo de loja, reparo).
    /// </summary>
    public static class ConfigEconomia
    {
        /// <summary>Preço base de venda para itens genéricos que não possuem tier de equipamento.</summary>
        public const long PrecoBaseItemGenerico = 1;

        /// <summary>Custo em coroas por ponto de durabilidade restaurado no ferreiro.</summary>
        public const long CustoReparoPorPontoPerdido = 1;

        /// <summary>Perda de durabilidade ao cair em combate (desgaste leve, ref. New World).</summary>
        public const int DesgasteAoCair = 5;

        /// <summary>
        /// Preço de venda fixo pago pelo NPC por tier de equipamento (sink de lixo).
        /// </summary>
        public static long PrecoVendaPorTier(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.D:
                    return 2;
                case TierItem.C:
                    return 5;
                case TierItem.B:
                    return 10;
                case TierItem.A:
                    return 25;
                case TierItem.S:
                    return 60;
                case TierItem.SMais:
                    return 100;
                default:
                    return PrecoBaseItemGenerico;
            }
        }

        /// <summary>
        /// Calcula o custo de reparo em coroas proporcional ao dano de durabilidade (sink leve).
        /// 1 coroa a cada 5 pontos de durabilidade restaurada (mínimo 1 se houver dano).
        /// </summary>
        public static long CalcularCustoReparo(int durabilidadeMaxima, int durabilidadeAtual)
        {
            int dano = Math.Max(0, durabilidadeMaxima - durabilidadeAtual);
            if (dano <= 0)
            {
                return 0;
            }
            return Math.Max(1, (dano + 4) / 5);
        }

        /// <summary>
        /// Catálogo padrão de itens vendidos pelo NPC.
        /// </summary>
        public static readonly ItemLojaDto[] CatalogoPadrao = new ItemLojaDto[]
        {
            new ItemLojaDto(
                CatalogoItens.PocaoDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.PocaoDeTeste) ?? "Poção",
                15
            ),
            new ItemLojaDto(
                CatalogoItens.EspadaDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.EspadaDeTeste) ?? "Espada",
                50
            ),
            new ItemLojaDto(
                CatalogoItens.EscudoDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.EscudoDeTeste) ?? "Escudo",
                50
            ),
            new ItemLojaDto(
                CatalogoItens.RedeasDoCorcelNoturno,
                CatalogoItens.ObterNome(CatalogoItens.RedeasDoCorcelNoturno) ?? "Montaria",
                500
            ),
            new ItemLojaDto(
                CatalogoItens.SinoDoCervoJovem,
                CatalogoItens.ObterNome(CatalogoItens.SinoDoCervoJovem) ?? "Pet",
                500
            ),
            new ItemLojaDto(
                CatalogoItens.RedeasDaMulaDeCarga,
                CatalogoItens.ObterNome(CatalogoItens.RedeasDaMulaDeCarga) ?? "Mula",
                80
            ),
        };

        /// <summary>
        /// Procura um item no catálogo da loja por ID.
        /// </summary>
        public static ItemLojaDto ObterItemLoja(int itemId)
        {
            for (int i = 0; i < CatalogoPadrao.Length; i++)
            {
                if (CatalogoPadrao[i].ItemId == itemId)
                {
                    return CatalogoPadrao[i];
                }
            }
            return null;
        }
    }
}
