using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.EntityFrameworkCore;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Recursos do personagem (rota <see cref="RotasCombate.Recursos"/>, com token). A conta vem
/// sempre do token; personagem inexistente e de outra conta respondem o mesmo 404.
/// </summary>
internal static class CombateRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapGet(RotasCombate.Recursos, RecursosAsync).RequireAuthorization();

    private static async Task<IResult> RecursosAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        RecursosEmMemoria recursos,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        var id =
            long.TryParse(
                personagemId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var lido
            )
            && lido > 0
                ? lido
                : 0;
        if (
            id == 0
            || !await db.Set<PersonagemEntidade>()
                .AnyAsync(p => p.Id == id && p.ContaId == contaId, cancelamento)
        )
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroCombate.PersonagemNaoEncontrado
            );
        }

        // Banco ocupado (ao ler o personagem pela primeira vez) sobe como exceção: o Core responde
        // 503 ServidorOcupado.
        return Results.Ok(await recursos.ConsultarAsync(id, cancelamento));
    }
}
