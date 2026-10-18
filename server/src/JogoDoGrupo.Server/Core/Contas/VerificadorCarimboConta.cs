using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Implementação com cache em memória (TTL de 30s) para validação rápida de tokens revogáveis (#517).
/// </summary>
public sealed class VerificadorCarimboConta(IServiceScopeFactory escopos) : IVerificadorCarimboConta
{
    private readonly ConcurrentDictionary<long, (string Carimbo, DateTimeOffset ExpiraEm)> _cache =
        new();
    private static readonly TimeSpan TempoCache = TimeSpan.FromSeconds(30);

    public async ValueTask<bool> CarimboValidoAsync(
        long contaId,
        string? carimboDoToken,
        CancellationToken ct = default
    )
    {
        if (contaId <= 0 || string.IsNullOrEmpty(carimboDoToken))
        {
            return false;
        }

        var agora = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(contaId, out var entrada) && entrada.ExpiraEm > agora)
        {
            return string.Equals(entrada.Carimbo, carimboDoToken, StringComparison.Ordinal);
        }

        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var conta = await db.Set<Conta>()
            .AsNoTracking()
            .Where(c => c.Id == contaId)
            .Select(c => new { c.CarimboSeguranca })
            .FirstOrDefaultAsync(ct);

        if (conta == null)
        {
            return false;
        }

        _cache[contaId] = (conta.CarimboSeguranca, agora.Add(TempoCache));
        return string.Equals(conta.CarimboSeguranca, carimboDoToken, StringComparison.Ordinal);
    }

    public void Invalidar(long contaId)
    {
        _cache.TryRemove(contaId, out _);
    }
}
