using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
    /// <summary>O que a pessoa lê sobre o loot: a dica da tecla e os avisos do HUD.</summary>
    public static class TextosDoLoot
    {
        /// <summary>Dica embaixo da tela quando há item ao alcance.</summary>
        public const string Dica = "F: pegar";

        /// <summary>Nome formatado do item no chão, incluindo o tier quando houver.</summary>
        public static string DescricaoItem(string nome, bool temTier, TierItem tier)
        {
            var nomeFinal = string.IsNullOrWhiteSpace(nome) ? "Item" : nome;
            return temTier ? $"{nomeFinal} (tier {RegrasTier.Letra(tier)})" : nomeFinal;
        }

        /// <summary>
        /// Monta o texto da dica de coleta no HUD.
        /// Se houver 1 item: "{tecla}: pegar — {nome} (tier {letra})"
        /// Se houver >1 itens: "{tecla}: pegar ({total} itens)"
        /// </summary>
        public static string FormatarDica(string tecla, ItemNoChao itemMaisProximo, int total)
        {
            var rotuloTecla = string.IsNullOrWhiteSpace(tecla) ? "F" : tecla;
            if (total <= 0 || itemMaisProximo == null)
            {
                return $"{rotuloTecla}: pegar";
            }

            if (total == 1)
            {
                var desc = DescricaoItem(
                    itemMaisProximo.Nome,
                    itemMaisProximo.TemTier,
                    itemMaisProximo.Tier
                );
                return $"{rotuloTecla}: pegar — {desc}";
            }

            return $"{rotuloTecla}: pegar ({total} itens)";
        }

        /// <summary>Monta o texto da dica de coleta para um único item específico.</summary>
        public static string FormatarDica(string tecla, ItemNoChao item) =>
            FormatarDica(tecla, item, 1);

        /// <summary>
        /// Aviso de item que entrou no inventário. Com tier (peça de equipamento), o aviso a
        /// mostra entre parênteses: "Você pegou Elmo do Vigia (tier B)."
        /// </summary>
        public static string Coletado(string nome, TierItem? tier = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "Você pegou um item.";
            }

            return tier.HasValue
                ? $"Você pegou {nome} (tier {RegrasTier.Letra(tier.Value)})."
                : $"Você pegou {nome}.";
        }

        /// <summary>Aviso de coleta recusada, a partir do código do servidor.</summary>
        public static string Recusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroItens.InventarioCheio:
                    return "Inventário cheio. O item fica no chão.";
                case CodigosErroItens.LongeDemais:
                    return "Chegue mais perto para pegar.";
                case CodigosErroItens.LootNaoEncontrado:
                    return "Nada para pegar aqui.";
                default:
                    return "Não deu para pegar o item.";
            }
        }
    }
}
