using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// A consulta de nome e id, respondida pelo banco a cada chamada (sem cache: personagem recém-
/// criado aparece na hora e apagado some na hora). Abre um escopo próprio por chamada, então
/// serve a quem vive fora de uma requisição, como os módulos do tempo real.
/// </summary>
internal sealed class DiretorioDePersonagens(IServiceScopeFactory escopos) : IDiretorioDePersonagens
{
    public bool TentarObterPorNome(string nome, out long personagemId)
    {
        personagemId = 0;
        if (RegrasPersonagem.ValidarNome(nome) != null)
        {
            return false;
        }

        var normalizado = Personagem.Normalizar(nome);
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var ids = db.Set<Personagem>()
            .Where(p => p.NomeNormalizado == normalizado)
            .Select(p => p.Id)
            .Take(1)
            .ToList();
        if (ids.Count == 0)
        {
            return false;
        }

        personagemId = ids[0];
        return true;
    }

    public bool TentarObterNome(long personagemId, out string nome)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var nomes = db.Set<Personagem>()
            .Where(p => p.Id == personagemId)
            .Select(p => p.Nome)
            .Take(1)
            .ToList();
        nome = nomes.Count == 0 ? "" : nomes[0];
        return nomes.Count != 0;
    }

    public bool TentarObterContaDoPersonagem(long personagemId, out long contaId)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var contas = db.Set<Personagem>()
            .Where(p => p.Id == personagemId)
            .Select(p => p.ContaId)
            .Take(1)
            .ToList();
        contaId = contas.Count == 0 ? 0 : contas[0];
        return contas.Count != 0;
    }
}
