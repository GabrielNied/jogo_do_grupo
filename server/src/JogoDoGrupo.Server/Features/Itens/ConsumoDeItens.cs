using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class ConsumoDeItens(JogoDbContext db) : IConsumoDeItens
{
    public async Task<int?> ConsumirConsumivelAsync(
        long personagemId,
        long instanciaId,
        int modeloId,
        CancellationToken c
    )
    {
        ItemInventario? item;
        if (instanciaId > 0)
        {
            item = await db.Set<ItemInventario>()
                .SingleOrDefaultAsync(
                    i => i.Id == instanciaId && i.PersonagemId == personagemId,
                    c
                );
            if (item == null || !Bolsa.EstaNaBolsa(item) || item.Conteiner != ConteinerItem.Bolsa)
            {
                return null;
            }

            if (modeloId > 0 && item.ItemId != modeloId)
            {
                return null;
            }
        }
        else
        {
            if (modeloId <= 0)
            {
                return null;
            }

            item = await db.Set<ItemInventario>()
                .Where(i =>
                    i.PersonagemId == personagemId
                    && i.Posicao == PosicaoEquipamento.Inventario
                    && i.Conteiner == ConteinerItem.Bolsa
                    && i.ItemId == modeloId
                )
                .OrderBy(i => i.Id)
                .FirstOrDefaultAsync(c);

            if (item == null)
            {
                return null;
            }
        }

        if (item.LerEquipamento()?.Favorito == true)
        {
            return null;
        }

        var consumidoId = item.ItemId;
        db.Set<ItemInventario>().Remove(item);
        await db.SaveChangesAsync(c);
        return consumidoId;
    }
}
