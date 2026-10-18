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
    }
}
