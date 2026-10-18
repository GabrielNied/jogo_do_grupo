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

        /// <summary>Catálogo do Armeiro (armas).</summary>
        public static readonly ItemLojaDto[] CatalogoArmeiro = new ItemLojaDto[]
        {
            new ItemLojaDto(
                CatalogoItens.EspadaDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.EspadaDeTeste) ?? "Espada de Teste",
                50
            ),
            new ItemLojaDto(174, "Arco de Caça", 50),
            new ItemLojaDto(175, "Cajado de Aprendiz", 50),
            new ItemLojaDto(CatalogoEquipamentos.CajadoDeFogoDeTeste, "Cajado Flamejante", 120),
        };

        /// <summary>Catálogo do Armadureiro (armaduras e peças defensivas).</summary>
        public static readonly ItemLojaDto[] CatalogoArmadureiro = new ItemLojaDto[]
        {
            new ItemLojaDto(
                CatalogoItens.EscudoDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.EscudoDeTeste) ?? "Escudo de Teste",
                45
            ),
            new ItemLojaDto(101, "Gibão de Couro", 60),
            new ItemLojaDto(102, "Perneiras de Couro", 50),
            new ItemLojaDto(103, "Botas de Viajante", 40),
            new ItemLojaDto(104, "Capuz Reforçado", 40),
        };

        /// <summary>Catálogo do Taverneiro (comida e bebida).</summary>
        public static readonly ItemLojaDto[] CatalogoTaverneiro = new ItemLojaDto[]
        {
            new ItemLojaDto(50, "Pão da Taverna", 5),
            new ItemLojaDto(51, "Caneca de Cerveja", 8),
            new ItemLojaDto(52, "Sopa de Legumes", 10),
            new ItemLojaDto(53, "Assado de Javali", 20),
        };

        /// <summary>Catálogo do Merceeiro (consumíveis e materiais básicos).</summary>
        public static readonly ItemLojaDto[] CatalogoMerceeiro = new ItemLojaDto[]
        {
            new ItemLojaDto(
                CatalogoItens.PocaoDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.PocaoDeTeste) ?? "Poção de Teste",
                15
            ),
            new ItemLojaDto(54, "Poção de Mana", 15),
            new ItemLojaDto(55, "Bandagem Simples", 5),
            new ItemLojaDto(56, "Pedra de Amolar", 10),
            new ItemLojaDto(
                CatalogoItens.MoedaDeTeste,
                CatalogoItens.ObterNome(CatalogoItens.MoedaDeTeste) ?? "Moeda de Teste",
                25
            ),
            new ItemLojaDto(
                CatalogoItens.RedeasDaMulaDeCarga,
                CatalogoItens.ObterNome(CatalogoItens.RedeasDaMulaDeCarga) ?? "Mula",
                80
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
        };

        /// <summary>
        /// Catálogo padrão de itens vendidos pelo NPC (legado / unificado).
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
            new ItemLojaDto(50, "Pão da Taverna", 5),
            new ItemLojaDto(51, "Caneca de Cerveja", 8),
            new ItemLojaDto(101, "Gibão de Couro", 60),
            new ItemLojaDto(174, "Arco de Caça", 50),
        };

        /// <summary>
        /// Obtém o catálogo de itens para uma categoria de loja específica.
        /// </summary>
        public static ItemLojaDto[] ObterCatalogo(CategoriaLoja categoria)
        {
            return categoria switch
            {
                CategoriaLoja.Armeiro => CatalogoArmeiro,
                CategoriaLoja.Armadureiro => CatalogoArmadureiro,
                CategoriaLoja.Taverneiro => CatalogoTaverneiro,
                CategoriaLoja.Merceeiro => CatalogoMerceeiro,
                _ => CatalogoPadrao,
            };
        }

        /// <summary>
        /// Procura um item no catálogo de uma loja específica.
        /// Retorna null se o item não fizer parte daquela categoria de loja.
        /// </summary>
        public static ItemLojaDto ObterItemLoja(CategoriaLoja categoria, int itemId)
        {
            var catalogo = ObterCatalogo(categoria);
            for (int i = 0; i < catalogo.Length; i++)
            {
                if (catalogo[i].ItemId == itemId)
                {
                    return catalogo[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Procura um item no catálogo unificado da loja por ID.
        /// </summary>
        public static ItemLojaDto ObterItemLoja(int itemId)
        {
            return ObterItemLoja(CategoriaLoja.Nenhuma, itemId);
        }

        /// <summary>
        /// Converte a categoria de loja no ID canônico de seu NPC no CatalogoNpcs.
        /// </summary>
        public static int ObterNpcId(CategoriaLoja categoria)
        {
            return categoria switch
            {
                CategoriaLoja.Armeiro => Npcs.CatalogoNpcs.IdArmeiro,
                CategoriaLoja.Armadureiro => Npcs.CatalogoNpcs.IdArmadureiro,
                CategoriaLoja.Taverneiro => Npcs.CatalogoNpcs.IdTaverneiro,
                CategoriaLoja.Merceeiro => Npcs.CatalogoNpcs.IdMerceeiro,
                _ => Npcs.CatalogoNpcs.IdVendedor,
            };
        }

        /// <summary>
        /// Obtém a categoria de loja correspondente a um ID de NPC.
        /// </summary>
        public static CategoriaLoja ObterCategoria(int npcId)
        {
            if (npcId == Npcs.CatalogoNpcs.IdArmeiro)
                return CategoriaLoja.Armeiro;
            if (npcId == Npcs.CatalogoNpcs.IdArmadureiro)
                return CategoriaLoja.Armadureiro;
            if (npcId == Npcs.CatalogoNpcs.IdTaverneiro)
                return CategoriaLoja.Taverneiro;
            if (npcId == Npcs.CatalogoNpcs.IdMerceeiro || npcId == Npcs.CatalogoNpcs.IdVendedor)
                return CategoriaLoja.Merceeiro;
            return CategoriaLoja.Nenhuma;
        }
    }
}
