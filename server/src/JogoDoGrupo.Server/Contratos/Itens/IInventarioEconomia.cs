using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

public sealed record ItemParaVenda(
    long InstanciaId,
    int ItemId,
    string Nome,
    TierItem Tier,
    int DurabilidadeAtual,
    int DurabilidadeMaxima,
    bool Favorito,
    bool Equipado
);

public sealed record ItemParaReparo(
    long InstanciaId,
    int ItemId,
    string Nome,
    int DurabilidadeAtual,
    int DurabilidadeMaxima,
    long CustoReparo
);

public interface IInventarioEconomia
{
    Task<ItemParaVenda?> ObterItemParaVendaAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    );

    Task<bool> RemoverItemVendidoAsync(long personagemId, long instanciaId, CancellationToken c);

    Task<bool> AdicionarItemCompradoAsync(
        long personagemId,
        int itemId,
        int quantidade,
        CancellationToken c
    );

    Task<ItemParaReparo?> ObterItemParaReparoAsync(
        long personagemId,
        long instanciaId,
        CancellationToken c
    );

    Task<bool> ConcluirReparoAsync(long personagemId, long instanciaId, CancellationToken c);

    Task<List<ItemParaReparo>> ObterItensDanificadosAsync(long personagemId, CancellationToken c);

    Task<int> RepararTodosAsync(long personagemId, CancellationToken c);

    Task DesgastarItensEquipadosAsync(long personagemId, int dano, CancellationToken c);
}
