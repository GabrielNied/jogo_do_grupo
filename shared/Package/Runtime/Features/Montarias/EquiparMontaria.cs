using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Client → servidor: equipa uma montaria da coleção para ser invocada.
    /// Id: <see cref="MontariasMessageIds.EquiparMontaria"/>.
    /// </summary>
    [Serializable]
    public class EquiparMontaria
    {
        public int TipoMontaria;
    }
}
