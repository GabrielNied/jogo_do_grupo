using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Instancias;

/// <summary>
/// Contrato do serviço de consulta e registro de trava semanal de loot de raid.
/// </summary>
public interface IBloqueioSemanalRaidServico
{
    Task<IReadOnlyCollection<int>> ObterEtapasConcluidasAsync(
        long personagemId,
        string chaveSemana,
        CancellationToken c = default
    );

    Task<bool> JaConcluiuEtapaAsync(
        long personagemId,
        string chaveSemana,
        int etapa,
        CancellationToken c = default
    );

    Task RegistrarConclusaoAsync(
        long personagemId,
        string chaveSemana,
        int etapa,
        DateTimeOffset momentoUtc,
        CancellationToken c = default
    );
}

public sealed class BloqueioSemanalRaidServico(JogoDbContext db) : IBloqueioSemanalRaidServico
{
    public async Task<IReadOnlyCollection<int>> ObterEtapasConcluidasAsync(
        long personagemId,
        string chaveSemana,
        CancellationToken c = default
    )
    {
        return await db.Set<BloqueioSemanalRaid>()
            .Where(b => b.PersonagemId == personagemId && b.ChaveSemana == chaveSemana)
            .Select(b => b.Etapa)
            .ToListAsync(c);
    }

    public async Task<bool> JaConcluiuEtapaAsync(
        long personagemId,
        string chaveSemana,
        int etapa,
        CancellationToken c = default
    )
    {
        return await db.Set<BloqueioSemanalRaid>()
            .AnyAsync(
                b =>
                    b.PersonagemId == personagemId
                    && b.ChaveSemana == chaveSemana
                    && b.Etapa == etapa,
                c
            );
    }

    public async Task RegistrarConclusaoAsync(
        long personagemId,
        string chaveSemana,
        int etapa,
        DateTimeOffset momentoUtc,
        CancellationToken c = default
    )
    {
        var existe = await JaConcluiuEtapaAsync(personagemId, chaveSemana, etapa, c);
        if (!existe)
        {
            db.Set<BloqueioSemanalRaid>()
                .Add(
                    new BloqueioSemanalRaid
                    {
                        PersonagemId = personagemId,
                        ChaveSemana = chaveSemana,
                        Etapa = etapa,
                        ConcluidoEm = momentoUtc,
                    }
                );
            await db.SaveChangesAsync(c);
        }
    }
}
