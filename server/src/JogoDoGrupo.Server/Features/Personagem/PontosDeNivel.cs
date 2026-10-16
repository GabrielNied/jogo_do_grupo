using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Pontos da árvore que o nível dá. A baixa é um <c>UPDATE</c> condicional ("só se ainda
/// há pontos"), então dois gastos ao mesmo tempo nunca usam o mesmo ponto.
/// </summary>
public sealed class PontosDeNivel(JogoDbContext db) : IPontosDeNivel
{
    public async Task<int> ObterAsync(long personagemId, CancellationToken cancelamento) =>
        await db.Set<Personagem>()
            .AsNoTracking()
            .Where(p => p.Id == personagemId)
            .Select(p => p.PontosLivres)
            .FirstOrDefaultAsync(cancelamento);

    public async Task<int> ObterNivelAsync(long personagemId, CancellationToken cancelamento)
    {
        var nivel = await db.Set<Personagem>()
            .AsNoTracking()
            .Where(p => p.Id == personagemId)
            .Select(p => p.Nivel)
            .FirstOrDefaultAsync(cancelamento);
        return nivel > 0 ? nivel : Personagem.NivelInicial;
    }

    public async Task<bool> TentarGastarAsync(
        long personagemId,
        int quantidade,
        CancellationToken cancelamento
    )
    {
        if (quantidade < 1)
        {
            return false;
        }

        var linhas = await db.Set<Personagem>()
            .Where(p => p.Id == personagemId && p.PontosLivres >= quantidade)
            .ExecuteUpdateAsync(
                p => p.SetProperty(x => x.PontosLivres, x => x.PontosLivres - quantidade),
                cancelamento
            );
        return linhas == 1;
    }

    public async Task DevolverAsync(
        long personagemId,
        int quantidade,
        CancellationToken cancelamento
    )
    {
        if (quantidade < 1)
        {
            return;
        }

        await db.Set<Personagem>()
            .Where(p => p.Id == personagemId)
            .ExecuteUpdateAsync(
                p => p.SetProperty(x => x.PontosLivres, x => x.PontosLivres + quantidade),
                cancelamento
            );
    }
}
