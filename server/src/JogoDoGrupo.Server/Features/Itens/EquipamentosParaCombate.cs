using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class BonusEquipamentos(IServiceScopeFactory escopos) : IBonusEquipamentos
{
    public async Task<AtributoItem[]> ObterAsync(long personagemId, CancellationToken c)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        // Mesmo snapshot para conjunto e peças, inclusive durante troca concorrente.
        await using var transacao = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            c
        );
        var conjunto =
            await db.Set<EstadoEquipamentos>()
                .AsNoTracking()
                .Where(e => e.PersonagemId == personagemId)
                .Select(e => (int?)e.ConjuntoAtivo)
                .SingleOrDefaultAsync(c)
            ?? 1;
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao != PosicaoEquipamento.Inventario
            )
            .ToListAsync(c);
        var bonus = RegrasEquipamentos.Somar(itens.Select(i => i.ParaEntrada()), conjunto);
        await transacao.CommitAsync(c);
        return bonus;
    }
}
