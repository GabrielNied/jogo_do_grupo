using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Pede ao servidor para usar um item consumível de montaria da bolsa, registrando-a na conta.
    /// </summary>
    [Serializable]
    public class UsarItemMontaria
    {
        public long InstanciaId;
    }
}
