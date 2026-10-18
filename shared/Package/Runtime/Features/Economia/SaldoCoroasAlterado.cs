using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: notifica o saldo atual de coroas do personagem.
    /// Id: <see cref="EconomiaMessageIds.SaldoCoroasAlterado"/>.
    /// </summary>
    [Serializable]
    public class SaldoCoroasAlterado
    {
        public long Saldo;

        public SaldoCoroasAlterado() { }

        public SaldoCoroasAlterado(long saldo)
        {
            Saldo = saldo;
        }
    }
}
