using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>IDs de mensagem de Economia. Use somente valores entre 9000 e 9999.</summary>
    public static class EconomiaMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(9000, 9999);

        /// <summary>Client → servidor: consulta o saldo de coroas do personagem.</summary>
        public const ushort ConsultarSaldoCoroas = 9000;

        /// <summary>Servidor → client: saldo de coroas do personagem atualizado.</summary>
        public const ushort SaldoCoroasAlterado = 9001;

        /// <summary>Client → servidor: consulta itens disponíveis na loja de NPC.</summary>
        public const ushort ConsultarCatalogoLoja = 9002;

        /// <summary>Servidor → client: resposta com o catálogo da loja.</summary>
        public const ushort RespostaCatalogoLoja = 9003;

        /// <summary>Client → servidor: pede compra de item da loja.</summary>
        public const ushort ComprarItemDaLoja = 9004;

        /// <summary>Servidor → client: notifica compra de item concluída.</summary>
        public const ushort ItemCompradoDaLoja = 9005;

        /// <summary>Client → servidor: pede venda de item do inventário para o NPC.</summary>
        public const ushort VenderItemParaNpc = 9006;

        /// <summary>Servidor → client: notifica venda de item concluída.</summary>
        public const ushort ItemVendidoParaNpc = 9007;

        /// <summary>Client → servidor: pede reparo de equipamento.</summary>
        public const ushort RepararItem = 9008;

        /// <summary>Servidor → client: notifica reparo concluído.</summary>
        public const ushort ItemReparado = 9009;

        /// <summary>Servidor → client: recusa de operação de economia com código de erro.</summary>
        public const ushort EconomiaRecusada = 9010;

        // ── Mercado (leilão por ordens, #176): 9100 a 9199 ────────────────────────

        /// <summary>Client → servidor: abre ou atualiza o mercado (perto do leiloeiro).</summary>
        public const ushort ConsultarMercado = 9100;

        /// <summary>Client → servidor: anuncia um exemplar da bolsa por um preço.</summary>
        public const ushort OferecerVenda = 9101;

        /// <summary>Client → servidor: pede unidades de um modelo por até um preço cada.</summary>
        public const ushort OferecerCompra = 9102;

        /// <summary>Client → servidor: compra já uma ordem de venda específica.</summary>
        public const ushort ComprarOrdem = 9103;

        /// <summary>Client → servidor: cancela uma ordem aberta própria.</summary>
        public const ushort CancelarOrdem = 9104;

        /// <summary>Client → servidor: pede os itens de ordens vencidas que não couberam na bolsa.</summary>
        public const ushort ReceberDevolucoes = 9105;

        /// <summary>Servidor → client: o mercado inteiro como o jogador o vê.</summary>
        public const ushort EstadoDoMercado = 9110;

        /// <summary>Servidor → client: uma ordem do jogador casou, venceu ou o item voltou.</summary>
        public const ushort AvisoDoMercado = 9111;

        // ── Barracas de Jogador e Vendedor Offline (#905): 9120 a 9129 ───────────

        /// <summary>Client → servidor: monta a barraca presencial na praça.</summary>
        public const ushort MontarBarraca = 9120;

        /// <summary>Client → servidor: desmonta a barraca presencial.</summary>
        public const ushort DesmontarBarraca = 9121;

        /// <summary>Client → servidor: contrata o manequim vendedor offline.</summary>
        public const ushort ContratarVendedorOffline = 9122;

        /// <summary>Client → servidor: encerra o vendedor offline.</summary>
        public const ushort EncerrarVendedorOffline = 9123;

        /// <summary>Client → servidor: consulta as ofertas e dados de uma barraca.</summary>
        public const ushort ConsultarBarraca = 9124;

        /// <summary>Servidor → client: vitrine e ofertas da barraca consultada.</summary>
        public const ushort DadosDaBarraca = 9125;

        /// <summary>Servidor → client: lista de barracas ativas na praça da cidade.</summary>
        public const ushort BarracasNaArea = 9126;

        /// <summary>Client → servidor: compra presencialmente um item da barraca (taxa de 3%).</summary>
        public const ushort ComprarDaBarraca = 9127;

        // ── Banco de Guilda (#338): 9220 a 9229 ─────────────────────────────────

        /// <summary>Client → servidor: consulta o saldo e itens do banco da guilda.</summary>
        public const ushort ConsultarBancoGuilda = 9220;

        /// <summary>Servidor → client: estado atualizado do banco da guilda.</summary>
        public const ushort EstadoDoBancoGuilda = 9221;

        /// <summary>Client → servidor: deposita item da bolsa no cofre da guilda.</summary>
        public const ushort DepositarItemBancoGuilda = 9222;

        /// <summary>Client → servidor: retira item do cofre da guilda para a bolsa.</summary>
        public const ushort RetirarItemBancoGuilda = 9223;

        /// <summary>Client → servidor: deposita coroas da carteira no cofre da guilda.</summary>
        public const ushort DepositarCoroasBancoGuilda = 9224;

        /// <summary>Client → servidor: retira coroas do cofre da guilda para a carteira.</summary>
        public const ushort RetirarCoroasBancoGuilda = 9225;

        /// <summary>Servidor → client: aviso ou evento sobre o banco da guilda.</summary>
        public const ushort AvisoBancoGuilda = 9226;
    }
}
