using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class InventarioEconomia(JogoDbContext db, GeradorEquipamentos gerador)
    : IInventarioEconomia
{
    public async Task<ItemParaVenda?> ObterItemParaVendaAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await db.Set<ItemInventario>()
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId, c);
        if (item == null)
            return null;

        var eq = item.LerEquipamento();
        var nome = CatalogoItens.ObterNome(item.ItemId) ?? eq?.Nome ?? "";
        var tier = eq != null ? RegrasTier.TierDe(eq) : TierItem.D;
        var durAtual = eq?.DurabilidadeAtual ?? 100;
        var durMax = eq?.DurabilidadeMaxima ?? 100;
        var favorito = eq?.Favorito ?? false;
        var equipado = item.Posicao != PosicaoEquipamento.Inventario;

        return new ItemParaVenda(
            item.Id,
            item.ItemId,
            nome,
            tier,
            durAtual,
            durMax,
            favorito,
            equipado
        );
    }

    public async Task<bool> RemoverItemVendidoAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await db.Set<ItemInventario>()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId, c);
        if (item == null || item.Posicao != PosicaoEquipamento.Inventario)
            return false;

        var eq = item.LerEquipamento();
        if (eq?.Favorito == true)
            return false;

        db.Set<ItemInventario>().Remove(item);
        await db.SaveChangesAsync(c);
        return true;
    }

    public async Task<bool> AdicionarItemCompradoAsync(
        long personagemId,
        int itemId,
        int quantidade,
        CancellationToken c
    )
    {
        if (quantidade <= 0)
            return false;

        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var existentes = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao == PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync(c);

        Bolsa.Normalizar(existentes, capacidade);

        var novos = new List<ItemInventario>();
        for (var i = 0; i < quantidade; i++)
        {
            novos.Add(new ItemInventario { PersonagemId = personagemId, ItemId = itemId });
        }

        if (!Bolsa.Guardar(existentes, novos, capacidade))
            return false;

        foreach (var novo in novos)
        {
            var eq = gerador.Gerar(novo.ItemId, Random.Shared);
            if (eq != null)
                novo.GravarEquipamento(eq);
        }

        db.Set<ItemInventario>().AddRange(novos);
        await db.SaveChangesAsync(c);
        return true;
    }

    public async Task<ItemParaReparo?> ObterItemParaReparoAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await db.Set<ItemInventario>()
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId, c);
        if (item == null)
            return null;

        var eq = item.LerEquipamento();
        if (eq == null)
            return null;

        var custo = ConfigEconomia.CalcularCustoReparo(eq.DurabilidadeMaxima, eq.DurabilidadeAtual);
        var nome = CatalogoItens.ObterNome(item.ItemId) ?? eq.Nome;

        return new ItemParaReparo(
            item.Id,
            item.ItemId,
            nome,
            eq.DurabilidadeAtual,
            eq.DurabilidadeMaxima,
            custo
        );
    }

    public async Task<bool> ConcluirReparoAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await db.Set<ItemInventario>()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId, c);
        if (item == null)
            return false;

        var eq = item.LerEquipamento();
        if (eq == null || eq.DurabilidadeAtual >= eq.DurabilidadeMaxima)
            return false;

        eq.DurabilidadeAtual = eq.DurabilidadeMaxima;
        item.GravarEquipamento(eq);
        await db.SaveChangesAsync(c);
        return true;
    }

    public async Task<List<ItemParaReparo>> ObterItensDanificadosAsync(
        long personagemId,
        CancellationToken c
    )
    {
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i => i.PersonagemId == personagemId && i.EquipamentoJson != null)
            .OrderBy(i => i.Id)
            .ToListAsync(c);

        var danificados = new List<ItemParaReparo>();
        foreach (var item in itens)
        {
            var eq = item.LerEquipamento();
            if (eq != null && eq.DurabilidadeAtual < eq.DurabilidadeMaxima)
            {
                var custo = ConfigEconomia.CalcularCustoReparo(
                    eq.DurabilidadeMaxima,
                    eq.DurabilidadeAtual
                );
                var nome = CatalogoItens.ObterNome(item.ItemId) ?? eq.Nome;
                danificados.Add(
                    new ItemParaReparo(
                        item.Id,
                        item.ItemId,
                        nome,
                        eq.DurabilidadeAtual,
                        eq.DurabilidadeMaxima,
                        custo
                    )
                );
            }
        }

        return danificados;
    }

    public async Task<int> RepararTodosAsync(long personagemId, CancellationToken c)
    {
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == personagemId && i.EquipamentoJson != null)
            .ToListAsync(c);

        var reparados = 0;
        foreach (var item in itens)
        {
            var eq = item.LerEquipamento();
            if (eq != null && eq.DurabilidadeAtual < eq.DurabilidadeMaxima)
            {
                eq.DurabilidadeAtual = eq.DurabilidadeMaxima;
                item.GravarEquipamento(eq);
                reparados++;
            }
        }

        if (reparados > 0)
            await db.SaveChangesAsync(c);

        return reparados;
    }

    public async Task DesgastarItensEquipadosAsync(long personagemId, int dano, CancellationToken c)
    {
        if (dano <= 0)
            return;

        var equipados = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId
                && i.Posicao != PosicaoEquipamento.Inventario
                && i.EquipamentoJson != null
            )
            .ToListAsync(c);

        var alterou = false;
        foreach (var item in equipados)
        {
            var eq = item.LerEquipamento();
            if (eq != null && eq.DurabilidadeAtual > 0)
            {
                eq.DurabilidadeAtual = Math.Max(0, eq.DurabilidadeAtual - dano);
                item.GravarEquipamento(eq);
                alterou = true;
            }
        }

        if (alterou)
            await db.SaveChangesAsync(c);
    }
}
