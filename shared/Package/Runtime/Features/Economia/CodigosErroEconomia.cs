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

        // Barracas e Vendedor Offline (#905)
        public const string ForaDaPraca = "fora_da_praca";
        public const string BarracaJaAberta = "barraca_ja_aberta";
        public const string BarracaNaoEncontrada = "barraca_nao_encontrada";
        public const string VendedorOfflineJaAtivo = "vendedor_offline_ja_ativo";
        public const string VendedorOfflineNaoAtivo = "vendedor_offline_nao_ativo";
        public const string DiariaInvalida = "diaria_invalida";
        public const string LongeDaBarraca = "longe_da_barraca";

        // Correio entre jogadores (#212)
        public const string DestinatarioNaoEncontrado = "destinatario_nao_encontrado";
        public const string DestinatarioInvalido = "destinatario_invalido";
        public const string CartaNaoEncontrada = "carta_nao_encontrada";
        public const string CartaComAnexos = "carta_com_anexos";
        public const string SemAnexos = "sem_anexos";
        public const string AssuntoVazio = "assunto_vazio";
        public const string LimiteDeAnexos = "limite_de_anexos";
        public const string LongeDoCorreio = "longe_do_correio";
    }
}
