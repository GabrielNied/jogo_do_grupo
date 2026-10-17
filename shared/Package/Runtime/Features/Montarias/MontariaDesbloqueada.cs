using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Notifica o client de que uma montaria foi desbloqueada e registrada na conta.
    /// </summary>
    [Serializable]
    public class MontariaDesbloqueada
    {
        public int TipoMontaria;
        public string Nome = "";
    }
}
