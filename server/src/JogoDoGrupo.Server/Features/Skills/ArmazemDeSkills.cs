using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Skills;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Skills;

/// <summary>Onde as skills de cada exemplar de arma ficam guardadas.</summary>
public interface IArmazemDeSkills
{
    /// <summary>
    /// Os ids das skills do exemplar, na ordem da barra. Na primeira vez sorteia do pool do
    /// modelo-base e grava; depois devolve sempre os mesmos. Se o gravado já não vale (catálogo
    /// ou modelo mudaram), sorteia de novo.
    /// </summary>
    Task<IReadOnlyList<string>> ObterOuSortearAsync(ArmaEquipada arma, CancellationToken c);
}

/// <summary>Sorteio de inteiros do servidor, trocável nos testes.</summary>
public interface ISorteadorDeSkills
{
    /// <summary>Inteiro de 0 (inclusive) a <paramref name="limite"/> (exclusive).</summary>
    int Proximo(int limite);
}

internal sealed class SorteadorDeSkills : ISorteadorDeSkills
{
    public int Proximo(int limite) => Random.Shared.Next(limite);
}

/// <summary>
/// Guarda as skills sorteadas por exemplar na tabela <see cref="SkillsDoExemplar"/>, com cópia em
/// memória. O sorteio e a gravação rodam sob uma trava só: dois pedidos para a mesma arma nova
/// nunca sorteiam duas vezes.
/// </summary>
public sealed class ArmazemDeSkills(IServiceScopeFactory escopos, ISorteadorDeSkills sorteador)
    : IArmazemDeSkills
{
    private readonly ConcurrentDictionary<long, (int ModeloId, string[] Ids)> _cache = new();
    private readonly SemaphoreSlim _sorteio = new(1, 1);

    public async Task<IReadOnlyList<string>> ObterOuSortearAsync(
        ArmaEquipada arma,
        CancellationToken c
    )
    {
        if (
            _cache.TryGetValue(arma.InstanciaId, out var guardado)
            && guardado.ModeloId == arma.ModeloId
            && RegrasSkills.IdsValidos(arma.Tipo, guardado.Ids)
        )
        {
            return guardado.Ids;
        }

        await _sorteio.WaitAsync(c);
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var linha = await db.Set<SkillsDoExemplar>()
                .FirstOrDefaultAsync(s => s.InstanciaId == arma.InstanciaId, c);
            if (linha != null && linha.ModeloId == arma.ModeloId)
            {
                var ids = linha.SkillIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (RegrasSkills.IdsValidos(arma.Tipo, ids))
                {
                    _cache[arma.InstanciaId] = (arma.ModeloId, ids);
                    return ids;
                }
            }

            var sorteadas = RegrasSkills.Sortear(arma.Tipo, sorteador.Proximo);
            if (linha == null)
            {
                db.Set<SkillsDoExemplar>()
                    .Add(
                        new SkillsDoExemplar
                        {
                            InstanciaId = arma.InstanciaId,
                            ModeloId = arma.ModeloId,
                            SkillIds = string.Join(',', sorteadas),
                        }
                    );
            }
            else
            {
                linha.ModeloId = arma.ModeloId;
                linha.SkillIds = string.Join(',', sorteadas);
            }

            await db.SaveChangesAsync(c);
            _cache[arma.InstanciaId] = (arma.ModeloId, sorteadas);
            return sorteadas;
        }
        finally
        {
            _sorteio.Release();
        }
    }
}
