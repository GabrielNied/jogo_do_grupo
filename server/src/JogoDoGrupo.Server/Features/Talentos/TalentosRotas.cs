using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>Ver a árvore e comprar rank (rotas de <see cref="RotasTalentos"/>, com token).</summary>
internal static class TalentosRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var grupo = rotas
            .MapGroup("/api/personagens/{personagemId}/talentos")
            .RequireAuthorization();
        grupo.MapGet("", ArvoreAsync);
        grupo.MapPost("/comprar", ComprarAsync);
    }

    private static async Task<IResult> ArvoreAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        IVerificadorPersonagem verificador,
        ServicoDaArvore servico,
        CancellationToken cancelamento
    )
    {
        var id = await PersonagemDaContaAsync(personagemId, usuario, verificador, cancelamento);
        return id == null
            ? PersonagemNaoEncontrado()
            : Results.Ok(await servico.ObterAsync(id.Value, cancelamento));
    }

    private static async Task<IResult> ComprarAsync(
        string personagemId,
        ComprarNoRequest? pedido,
        ClaimsPrincipal usuario,
        IVerificadorPersonagem verificador,
        ServicoDaArvore servico,
        CancellationToken cancelamento
    )
    {
        var id = await PersonagemDaContaAsync(personagemId, usuario, verificador, cancelamento);
        if (id == null)
        {
            return PersonagemNaoEncontrado();
        }

        if (pedido == null || string.IsNullOrEmpty(pedido.NoId))
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroTalentos.NoInvalido
            );
        }

        var resultado = await servico.ComprarAsync(id.Value, pedido, cancelamento);
        return resultado.Erro == null ? Results.Ok(resultado.Arvore) : Recusa(resultado.Erro);
    }

    private static IResult Recusa(string erro) =>
        RespostasApi.Erro(
            erro is CodigosErroTalentos.NoInvalido or CodigosErroTalentos.RankAlvoInvalido
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status409Conflict,
            erro
        );

    // Só o dono do personagem (a conta do token) mexe na árvore.
    private static async Task<long?> PersonagemDaContaAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        IVerificadorPersonagem verificador,
        CancellationToken cancelamento
    )
    {
        if (
            !long.TryParse(
                personagemId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var id
            )
            || id <= 0
        )
        {
            return null;
        }

        return await verificador.PertenceAContaAsync(usuario.ObterContaId(), id, cancelamento)
            ? id
            : null;
    }

    private static IResult PersonagemNaoEncontrado() =>
        RespostasApi.Erro(
            StatusCodes.Status404NotFound,
            CodigosErroPersonagem.PersonagemNaoEncontrado
        );
}
