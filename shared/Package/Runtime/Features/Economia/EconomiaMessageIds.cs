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
    }
}
