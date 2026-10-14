using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>Rotas HTTP da árvore de talentos, redistribuição e perfis de build (com token).</summary>
internal static class TalentosRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var grupo = rotas
            .MapGroup("/api/personagens/{personagemId}/talentos")
            .RequireAuthorization();

        grupo.MapGet("", ArvoreAsync);
        grupo.MapPost("/comprar", ComprarAsync);
        grupo.MapPost("/redistribuir", RedistribuirAsync);
        grupo.MapGet("/perfis", ListarPerfisAsync);
        grupo.MapPost("/perfis", SalvarPerfilAsync);
        grupo.MapPut("/perfis/{perfilId}", RenomearPerfilAsync);
        grupo.MapDelete("/perfis/{perfilId}", ExcluirPerfilAsync);
        grupo.MapPost("/perfis/{perfilId}/carregar", CarregarPerfilAsync);
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

    private static async Task<IResult> RedistribuirAsync(
        string personagemId,
        RedistribuirRequest? pedido,
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

        if (pedido == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroTalentos.ArvoreDesatualizada
            );
        }

        var resultado = await servico.RedistribuirAsync(id.Value, pedido, cancelamento);
        return resultado.Erro == null ? Results.Ok(resultado.Arvore) : Recusa(resultado.Erro);
    }

    private static async Task<IResult> ListarPerfisAsync(
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
            : Results.Ok(await servico.ListarPerfisAsync(id.Value, cancelamento));
    }

    private static async Task<IResult> SalvarPerfilAsync(
        string personagemId,
        SalvarPerfilRequest? pedido,
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

        if (pedido == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroTalentos.NomeDePerfilInvalido
            );
        }

        var resultado = await servico.SalvarPerfilAsync(id.Value, pedido, cancelamento);
        return resultado.Erro == null ? Results.Ok(resultado.Perfil) : Recusa(resultado.Erro);
    }

    private static async Task<IResult> RenomearPerfilAsync(
        string personagemId,
        string perfilId,
        RenomearPerfilRequest? pedido,
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

        if (
            !long.TryParse(perfilId, NumberStyles.None, CultureInfo.InvariantCulture, out var pId)
            || pId <= 0
        )
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroTalentos.PerfilNaoEncontrado
            );
        }

        if (pedido == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroTalentos.NomeDePerfilInvalido
            );
        }

        var resultado = await servico.RenomearPerfilAsync(id.Value, pId, pedido, cancelamento);
        return resultado.Erro == null ? Results.Ok() : Recusa(resultado.Erro);
    }

    private static async Task<IResult> ExcluirPerfilAsync(
        string personagemId,
        string perfilId,
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

        if (
            !long.TryParse(perfilId, NumberStyles.None, CultureInfo.InvariantCulture, out var pId)
            || pId <= 0
        )
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroTalentos.PerfilNaoEncontrado
            );
        }

        var resultado = await servico.ExcluirPerfilAsync(id.Value, pId, cancelamento);
        return resultado.Erro == null ? Results.Ok() : Recusa(resultado.Erro);
    }

    private static async Task<IResult> CarregarPerfilAsync(
        string personagemId,
        string perfilId,
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

        if (
            !long.TryParse(perfilId, NumberStyles.None, CultureInfo.InvariantCulture, out var pId)
            || pId <= 0
        )
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroTalentos.PerfilNaoEncontrado
            );
        }

        var resultado = await servico.CarregarPerfilAsync(id.Value, pId, cancelamento);
        return resultado.Erro == null ? Results.Ok(resultado.Resposta) : Recusa(resultado.Erro);
    }

    private static IResult Recusa(string erro) =>
        RespostasApi.Erro(
            erro
                    is CodigosErroTalentos.NoInvalido
                        or CodigosErroTalentos.RankAlvoInvalido
                        or CodigosErroTalentos.NenhumPontoGasto
                        or CodigosErroTalentos.NomeDePerfilInvalido
                    ? StatusCodes.Status400BadRequest
                : erro == CodigosErroTalentos.PerfilNaoEncontrado ? StatusCodes.Status404NotFound
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
