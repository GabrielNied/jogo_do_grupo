using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Números e contas puras do mercado (leilão por ordens, #176). Regras em
    /// docs/features/economia.md; o servidor decide, o client só mostra (taxa, validade, limites).
    /// </summary>
    public static class ConfigMercado
    {
        // ── Balanceamento (propostas, calibráveis aqui) ───────────────────────────

        /// <summary>
        /// Taxa cobrada do vendedor em cada venda, em porcentagem do preço. Anunciar é grátis:
        /// não há taxa ao abrir ordem (decidido por Gabriel em 2026-10-16).
        /// </summary>
        public const int TaxaVendaPercentual = 5;

        /// <summary>Dias que uma ordem fica aberta antes de expirar.</summary>
        public const int ExpiracaoDias = 7;

        /// <summary>Segundos que uma ordem fica aberta antes de expirar.</summary>
        public const long ExpiracaoSegundos = ExpiracaoDias * 24L * 60L * 60L;

        /// <summary>Menor preço por unidade (com a taxa arredondada para cima, o vendedor recebe 1).</summary>
        public const long PrecoMinimo = 2;

        /// <summary>Maior preço por unidade (evita estouro nas contas).</summary>
        public const long PrecoMaximo = 1000000000;

        /// <summary>Unidades que uma ordem de compra pode pedir.</summary>
        public const int QuantidadeMaximaDaCompra = 99;

        /// <summary>Ordens abertas ao mesmo tempo por personagem (compra e venda juntas).</summary>
        public const int OrdensAbertasMaximas = 20;

        /// <summary>Linhas de cada lista (vendas, compras) que o servidor manda ao client.</summary>
        public const int LinhasPorLista = 100;

        /// <summary>Segundos entre as varreduras que expiram ordens vencidas.</summary>
        public const int IntervaloDaVarreduraSegundos = 60;

        // ── O leiloeiro (NPC da cidade) ───────────────────────────────────────────

        /// <summary>Área onde o leiloeiro está (a cidade).</summary>
        public const string Area = Npcs.CatalogoNpcs.AreaCidade;

        /// <summary>Posição X do leiloeiro na cidade, perto do banqueiro.</summary>
        public const float LeiloeiroX = 6f;

        /// <summary>Posição Z do leiloeiro na cidade.</summary>
        public const float LeiloeiroZ = -14f;

        /// <summary>Distância horizontal máxima, em metros, que o servidor aceita até o leiloeiro.</summary>
        public const float RaioParaInteragirMetros = 4f;

        public static bool DentroDoRaioLeiloeiro(float x, float z)
        {
            var dx = x - LeiloeiroX;
            var dz = z - LeiloeiroZ;
            return dx * dx + dz * dz <= RaioParaInteragirMetros * RaioParaInteragirMetros;
        }

        // ── Contas ────────────────────────────────────────────────────────────────

        /// <summary>
        /// A taxa de uma venda de <paramref name="preco"/> coroas, arredondada para cima (a menor
        /// venda ainda paga 1). Cobrada uma vez, do vendedor, e destruída.
        /// </summary>
        public static long TaxaDaVenda(long preco)
        {
            if (preco <= 0)
            {
                return 0;
            }

            return (preco * TaxaVendaPercentual + 99) / 100;
        }

        /// <summary>O que o vendedor recebe de uma venda de <paramref name="preco"/> coroas.</summary>
        public static long LiquidoDaVenda(long preco) => preco - TaxaDaVenda(preco);

        /// <summary>Se o preço por unidade está dentro dos limites.</summary>
        public static bool PrecoValido(long preco) => preco >= PrecoMinimo && preco <= PrecoMaximo;

        // ── Itens negociáveis ─────────────────────────────────────────────────────

        private static Dictionary<int, string> _nomes;

        /// <summary>Id e nome de cada item que o mercado conhece (para pedir uma compra).</summary>
        public static IReadOnlyDictionary<int, string> ItensConhecidos
        {
            get
            {
                if (_nomes != null)
                {
                    return _nomes;
                }

                var nomes = new Dictionary<int, string>();
                foreach (var id in CatalogoItens.Todos)
                {
                    var nome = CatalogoItens.ObterNome(id);
                    if (nome != null)
                    {
                        nomes[id] = nome;
                    }
                }

                foreach (var id in CatalogoEquipamentos.ModelosDeTeste)
                {
                    var modelo = CatalogoEquipamentos.Obter(id);
                    if (modelo != null && !nomes.ContainsKey(id))
                    {
                        nomes[id] = modelo.Nome;
                    }
                }

                foreach (
                    var catalogo in new[]
                    {
                        ConfigEconomia.CatalogoArmeiro,
                        ConfigEconomia.CatalogoArmadureiro,
                        ConfigEconomia.CatalogoTaverneiro,
                        ConfigEconomia.CatalogoMerceeiro,
                        ConfigEconomia.CatalogoPadrao,
                    }
                )
                {
                    foreach (var item in catalogo)
                    {
                        if (!nomes.ContainsKey(item.ItemId))
                        {
                            nomes[item.ItemId] = item.Nome;
                        }
                    }
                }

                _nomes = nomes;
                return nomes;
            }
        }

        /// <summary>O nome do item, ou <c>null</c> se o mercado não o conhece.</summary>
        public static string NomeDoItem(int itemId) =>
            ItensConhecidos.TryGetValue(itemId, out var nome) ? nome : null;
    }
}
