using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Mensagem do servidor publicando a lista de NPCs ativos na área do jogador (#1063).</summary>
    [Serializable]
    public class NpcsDaArea
    {
        public string Area;
        public DefinicaoNpc[] Npcs;
    }
}
