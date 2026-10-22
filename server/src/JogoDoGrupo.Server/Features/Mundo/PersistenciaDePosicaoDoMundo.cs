using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Persistência de posição do jogador entre sessões no banco de dados (issue #1203).
/// </summary>
public sealed class PersistenciaDePosicaoDoMundo : IPersistenciaDePosicaoDoMundo
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<PersistenciaDePosicaoDoMundo>? _log;

    public PersistenciaDePosicaoDoMundo(
        IServiceScopeFactory escopos,
        ILogger<PersistenciaDePosicaoDoMundo>? log = null
    )
    {
        _escopos = escopos;
        _log = log;
    }

    public PosicaoDoPersonagem? Carregar(long personagemId)
    {
        try
        {
            using var escopo = _escopos.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            return db.Set<PosicaoDoPersonagem>()
                .AsNoTracking()
                .FirstOrDefault(p => p.PersonagemId == personagemId);
        }
        catch (Exception ex)
        {
            _log?.LogError(
                ex,
                "Erro ao carregar posicao do personagem {PersonagemId}",
                personagemId
            );
            return null;
        }
    }

    public async Task<PosicaoDoPersonagem?> CarregarAsync(
        long personagemId,
        CancellationToken ct = default
    )
    {
        try
        {
            using var escopo = _escopos.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            return await db.Set<PosicaoDoPersonagem>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PersonagemId == personagemId, ct);
        }
        catch (Exception ex)
        {
            _log?.LogError(
                ex,
                "Erro ao carregar posicao do personagem {PersonagemId}",
                personagemId
            );
            return null;
        }
    }

    public void Salvar(long personagemId, string area, float x, float y, float z, float rotacaoY)
    {
        try
        {
            using var escopo = _escopos.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var linha = db.Set<PosicaoDoPersonagem>()
                .FirstOrDefault(p => p.PersonagemId == personagemId);

            if (linha == null)
            {
                linha = new PosicaoDoPersonagem { PersonagemId = personagemId };
                db.Set<PosicaoDoPersonagem>().Add(linha);
            }

            linha.Area = area;
            linha.X = x;
            linha.Y = y;
            linha.Z = z;
            linha.RotacaoY = rotacaoY;
            linha.AtualizadoEm = DateTime.UtcNow;

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Erro ao salvar posicao do personagem {PersonagemId}", personagemId);
        }
    }

    public async Task SalvarAsync(
        long personagemId,
        string area,
        float x,
        float y,
        float z,
        float rotacaoY,
        CancellationToken ct = default
    )
    {
        try
        {
            using var escopo = _escopos.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var linha = await db.Set<PosicaoDoPersonagem>()
                .FirstOrDefaultAsync(p => p.PersonagemId == personagemId, ct);

            if (linha == null)
            {
                linha = new PosicaoDoPersonagem { PersonagemId = personagemId };
                db.Set<PosicaoDoPersonagem>().Add(linha);
            }

            linha.Area = area;
            linha.X = x;
            linha.Y = y;
            linha.Z = z;
            linha.RotacaoY = rotacaoY;
            linha.AtualizadoEm = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Erro ao salvar posicao do personagem {PersonagemId}", personagemId);
        }
    }
}
