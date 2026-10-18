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
    }
}
