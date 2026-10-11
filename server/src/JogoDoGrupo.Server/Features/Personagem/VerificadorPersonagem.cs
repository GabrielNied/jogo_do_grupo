using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>Confirma que o personagem pertence à conta do token (autenticação do tempo real).</summary>
public sealed class VerificadorPersonagem(JogoDbContext db) : IVerificadorPersonagem
{
    public Task<bool> PertenceAContaAsync(
        long contaId,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        db.Set<Personagem>()
            .AnyAsync(p => p.Id == personagemId && p.ContaId == contaId, cancelamento);
}
