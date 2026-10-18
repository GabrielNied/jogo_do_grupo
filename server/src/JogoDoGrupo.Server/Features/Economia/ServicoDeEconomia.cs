using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class ServicoDeEconomia(
    JogoDbContext db,
    ICarteiraCoroas carteira,
    IInventarioEconomia inventario
)
{
    public async Task<long> ObterSaldoAsync(long personagemId, CancellationToken c) =>
        await carteira.SaldoAsync(personagemId, c);

    public ItemLojaDto[] ObterCatalogo(CategoriaLoja categoria = CategoriaLoja.Nenhuma) =>
        ConfigEconomia.ObterCatalogo(categoria);

    public Task<(bool Sucesso, string? Erro, ItemCompradoDaLoja? Resultado)> ComprarItemAsync(
        long personagemId,
        int itemId,
        int quantidade,
        CancellationToken c
    ) => ComprarItemAsync(personagemId, itemId, quantidade, CategoriaLoja.Nenhuma, c);

    public async Task<(bool Sucesso, string? Erro, ItemCompradoDaLoja? Resultado)> ComprarItemAsync(
        long personagemId,
        int itemId,
        int quantidade,
        CategoriaLoja categoria,
        CancellationToken c
    )
    {
        if (quantidade <= 0)
            return (false, CodigosErroEconomia.QuantidadeInvalida, null);

        var itemLoja = ConfigEconomia.ObterItemLoja(categoria, itemId);
        if (itemLoja == null)
            return (false, CodigosErroEconomia.ItemNaoAVenda, null);

        long custoTotal = checked(itemLoja.PrecoCoroas * quantidade);
        var saldoAtual = await carteira.SaldoAsync(personagemId, c);
        if (saldoAtual < custoTotal)
            return (false, CodigosErroEconomia.SaldoInsuficiente, null);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );

        var debitou = await carteira.DebitarAsync(personagemId, custoTotal, c);
        if (!debitou)
            return (false, CodigosErroEconomia.SaldoInsuficiente, null);

        var adicionou = await inventario.AdicionarItemCompradoAsync(
            personagemId,
            itemId,
            quantidade,
            c
        );
        if (!adicionou)
            return (false, CodigosErroEconomia.InventarioCheio, null);

        await transacao.CommitAsync(c);
        var novoSaldo = await carteira.SaldoAsync(personagemId, c);

        return (true, null, new ItemCompradoDaLoja(itemId, quantidade, custoTotal, novoSaldo));
    }

    public async Task<(bool Sucesso, string? Erro, ItemVendidoParaNpc? Resultado)> VenderItemAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var item = await inventario.ObterItemParaVendaAsync(personagemId, instanciaId, c);
        if (item == null)
            return (false, CodigosErroEconomia.ItemNaoEncontrado, null);

        if (item.Equipado)
            return (false, CodigosErroEconomia.ItemEquipado, null);

        if (item.Favorito)
            return (false, CodigosErroEconomia.ItemFavorito, null);

        var preco = ConfigEconomia.PrecoVendaPorTier(item.Tier);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );

        var removeu = await inventario.RemoverItemVendidoAsync(personagemId, instanciaId, c);
        if (!removeu)
            return (false, CodigosErroEconomia.ItemNaoEncontrado, null);

        await carteira.CreditarAsync(personagemId, preco, c);
        await transacao.CommitAsync(c);
        var novoSaldo = await carteira.SaldoAsync(personagemId, c);

        return (true, null, new ItemVendidoParaNpc(instanciaId, item.ItemId, preco, novoSaldo));
    }

    public async Task<(bool Sucesso, string? Erro, ItemReparado? Resultado)> RepararItemAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    )
    {
        var reparo = await inventario.ObterItemParaReparoAsync(personagemId, instanciaId, c);
        if (reparo == null)
            return (false, CodigosErroEconomia.ItemNaoReparavel, null);

        if (reparo.CustoReparo <= 0 || reparo.DurabilidadeAtual >= reparo.DurabilidadeMaxima)
            return (false, CodigosErroEconomia.ItemJaReparado, null);

        var saldo = await carteira.SaldoAsync(personagemId, c);
        if (saldo < reparo.CustoReparo)
            return (false, CodigosErroEconomia.SaldoInsuficiente, null);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );

        var debitou = await carteira.DebitarAsync(personagemId, reparo.CustoReparo, c);
        if (!debitou)
            return (false, CodigosErroEconomia.SaldoInsuficiente, null);

        var reparou = await inventario.ConcluirReparoAsync(personagemId, instanciaId, c);
        if (!reparou)
            return (false, CodigosErroEconomia.ItemJaReparado, null);

        await transacao.CommitAsync(c);
        var novoSaldo = await carteira.SaldoAsync(personagemId, c);

        return (
            true,
            null,
            new ItemReparado(instanciaId, reparo.DurabilidadeMaxima, reparo.CustoReparo, novoSaldo)
        );
    }

    public async Task<(
        bool Sucesso,
        string? Erro,
        long CustoTotal,
        long SaldoRestante,
        int TotalReparados
    )> RepararTudoAsync(long personagemId, CancellationToken c)
    {
        var danificados = await inventario.ObterItensDanificadosAsync(personagemId, c);
        if (danificados.Count == 0)
            return (
                false,
                CodigosErroEconomia.ItemNaoDanificado,
                0,
                await carteira.SaldoAsync(personagemId, c),
                0
            );

        long custoTotal = 0;
        foreach (var d in danificados)
            custoTotal += d.CustoReparo;

        var saldo = await carteira.SaldoAsync(personagemId, c);
        if (saldo < custoTotal)
            return (false, CodigosErroEconomia.SaldoInsuficiente, 0, saldo, 0);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );

        var debitou = await carteira.DebitarAsync(personagemId, custoTotal, c);
        if (!debitou)
            return (false, CodigosErroEconomia.SaldoInsuficiente, 0, saldo, 0);

        var reparados = await inventario.RepararTodosAsync(personagemId, c);
        await transacao.CommitAsync(c);
        var novoSaldo = await carteira.SaldoAsync(personagemId, c);

        return (true, null, custoTotal, novoSaldo, reparados);
    }
}
