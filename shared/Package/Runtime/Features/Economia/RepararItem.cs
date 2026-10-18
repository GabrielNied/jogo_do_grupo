using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede reparo de um exemplar danificado.
    /// Id: <see cref="EconomiaMessageIds.RepararItem"/>.
    /// </summary>
    [Serializable]
    public class RepararItem
    {
        public long InstanciaId;

        public RepararItem() { }

        public RepararItem(long instanciaId)
        {
            InstanciaId = instanciaId;
        }
    }
}
