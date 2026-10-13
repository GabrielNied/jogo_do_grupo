using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>
/// Soma dos atributos dos nós comprados, para Combate. Singleton: cada consulta abre o seu
/// escopo e lê só os ranks do personagem.
/// </summary>
public sealed class AtributosDaArvoreDoPersonagem(
    IServiceScopeFactory escopos,
    ConfiguracaoDaArvore configuracao
) : IAtributosDaArvore
{
    public async Task<AtributosDaArvore> ObterAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var nos = await db.Set<NoComprado>()
            .AsNoTracking()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);
        var ranks = nos.ToDictionary(n => n.NoId, n => n.Rank);
        return RegrasDaArvore.SomarAtributos(configuracao, ranks);
    }
}
