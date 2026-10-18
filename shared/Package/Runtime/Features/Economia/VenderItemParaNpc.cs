using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede venda de um exemplar do inventário para o NPC.
    /// Id: <see cref="EconomiaMessageIds.VenderItemParaNpc"/>.
    /// </summary>
    [Serializable]
    public class VenderItemParaNpc
    {
        public long InstanciaId;

        public VenderItemParaNpc() { }

        public VenderItemParaNpc(long instanciaId)
        {
            InstanciaId = instanciaId;
        }
    }
}
