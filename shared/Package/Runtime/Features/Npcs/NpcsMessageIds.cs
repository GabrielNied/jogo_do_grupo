using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>IDs de mensagem de NPCs. Use somente valores entre 2000 e 2999.</summary>
    public static class NpcsMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(2000, 2999);

        /// <summary>Servidor → client: lista de NPCs ativos na área do jogador (#1063).</summary>
        public const ushort NpcsDaArea = 2001;

        /// <summary>Client → servidor: solicita a lista de NPCs da área atual (#1063).</summary>
        public const ushort SolicitarNpcs = 2002;

        /// <summary>Client → servidor: inicia diálogo com o NPC (#178).</summary>
        public const ushort FalarComNpc = 2010;

        /// <summary>Servidor → client: envia texto de fala e opções do NPC (#178).</summary>
        public const ushort DialogoDoNpc = 2011;

        /// <summary>Client → servidor: escolhe e executa uma opção de diálogo (#178).</summary>
        public const ushort ExecutarOpcaoDeDialogo = 2012;

        /// <summary>Servidor → client: recusa o diálogo (ex: longe demais) (#178).</summary>
        public const ushort DialogoRecusado = 2013;
    }
}
