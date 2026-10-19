using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Custódia de itens para troca direta e mercado. Retirar apaga a linha da bolsa e devolver cria
/// uma nova com a mesma cópia do exemplar (o mesmo molde do banco pessoal), então o exemplar
/// nunca existe em dois lugares.
/// </summary>
internal sealed class CustodiaDeItens(JogoDbContext db) : ICustodiaDeItens
{
    public async Task<List<EntradaInventario>> ListarNegociaveisAsync(
        long personagemId,
        CancellationToken c
    )
    {
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId
                && i.Posicao == PosicaoEquipamento.Inventario
                && i.Conteiner == ConteinerItem.Bolsa
            )
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        return itens
            .Where(i => i.LerEquipamento()?.Favorito != true)
            .Select(i => i.ParaEntrada())
            .ToList();
    }

    public async Task<ItemEmCustodia?> RetirarAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await db.Set<ItemInventario>()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId, c);
        if (item == null || !Bolsa.EstaNaBolsa(item) || item.Conteiner != ConteinerItem.Bolsa)
            return null;
        if (item.LerEquipamento()?.Favorito == true)
            return null;

        var custodia = new ItemEmCustodia(item.ItemId, item.EquipamentoJson);
        db.Set<ItemInventario>().Remove(item);
        await db.SaveChangesAsync(c);
        return custodia;
    }

    public async Task<bool> DevolverAsync(
        long personagemId,
        ItemEmCustodia item,
        CancellationToken c
    )
    {
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var existentes = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao == PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        Bolsa.Normalizar(existentes, capacidade);

        var novo = new ItemInventario
        {
            PersonagemId = personagemId,
            ItemId = item.ItemId,
            EquipamentoJson = item.EquipamentoJson,
        };
        if (!Bolsa.Guardar(existentes, new[] { novo }, capacidade))
            return false;

        db.Set<ItemInventario>().Add(novo);
        await db.SaveChangesAsync(c);
        return true;
    }

    public EntradaInventario Descrever(ItemEmCustodia item, long instanciaId = 0)
    {
        var linha = new ItemInventario
        {
            Id = instanciaId,
            ItemId = item.ItemId,
            EquipamentoJson = item.EquipamentoJson,
        };
        return linha.ParaEntrada();
    }
}
