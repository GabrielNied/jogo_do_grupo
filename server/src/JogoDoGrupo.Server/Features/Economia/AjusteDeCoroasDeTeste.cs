using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Ajuste de coroas da tela de teste. Usa a carteira de sempre (<see cref="ICarteiraCoroas"/>)
/// dentro de uma transação, então o saldo segue a mesma regra de nunca ficar negativo.
/// </summary>
internal sealed class AjusteDeCoroasDeTeste(JogoDbContext db, ICarteiraCoroas carteira)
    : IAjusteDeCoroasDeTeste
{
    public async Task<(bool Aceito, string Texto)> AjustarAsync(
        long personagemId,
        long quantidade,
        CancellationToken c
    )
    {
        if (quantidade == 0)
            return (false, "Quantidade zero.");

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        var saldo = await carteira.SaldoAsync(personagemId, c);
        if (quantidade > 0)
        {
            await carteira.CreditarAsync(personagemId, quantidade, c);
        }
        else
        {
            var tirar = Math.Min(saldo, -quantidade);
            if (tirar > 0 && !await carteira.DebitarAsync(personagemId, tirar, c))
                return (false, "O saldo mudou durante o pedido; tente de novo.");
        }

        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);
        var novo = await carteira.SaldoAsync(personagemId, c);
        return (true, $"Coroas: {saldo} → {novo}.");
    }
}
