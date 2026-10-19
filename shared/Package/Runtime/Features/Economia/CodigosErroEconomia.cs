namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Motivos padronizados de recusa para operações de Economia (compra, venda, reparo, carteira).
    /// </summary>
    public static class CodigosErroEconomia
    {
        public const string SaldoInsuficiente = "saldo_insuficiente";
        public const string InventarioCheio = "inventario_cheio";
        public const string ItemNaoEncontrado = "item_nao_encontrado";
        public const string ItemFavorito = "item_favorito";
        public const string ItemEquipado = "item_equipado";
        public const string ItemJaReparado = "item_ja_reparado";
        public const string ItemNaoDanificado = "item_nao_danificado";
        public const string ItemNaoAVenda = "item_nao_a_venda";
        public const string ItemNaoReparavel = "item_nao_reparavel";
        public const string QuantidadeInvalida = "quantidade_invalida";
        public const string PersonagemNaoEncontrado = "personagem_nao_encontrado";
        public const string LongeDoNpc = "longe_do_npc";

        // Mercado (#176)
        public const string PrecoInvalido = "preco_invalido";
        public const string LimiteDeOrdens = "limite_de_ordens";
        public const string OrdemNaoEncontrada = "ordem_nao_encontrada";
        public const string OrdemIndisponivel = "ordem_indisponivel";
        public const string OrdemPropria = "ordem_propria";
        public const string ItemNaoNegociavel = "item_nao_negociavel";
        public const string LongeDoLeiloeiro = "longe_do_leiloeiro";
    }
}
