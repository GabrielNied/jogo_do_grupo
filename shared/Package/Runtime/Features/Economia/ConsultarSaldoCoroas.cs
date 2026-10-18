using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede consulta do saldo de coroas do personagem.
    /// Id: <see cref="EconomiaMessageIds.ConsultarSaldoCoroas"/>.
    /// </summary>
    [Serializable]
    public class ConsultarSaldoCoroas { }
}
