using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Economia;

internal static class EconomiaRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet(RotasEconomia.Saldo, ObterSaldoAsync).RequireAuthorization();
        rotas.MapGet(RotasEconomia.Loja, ObterCatalogoAsync).RequireAuthorization();
        rotas.MapPost(RotasEconomia.Comprar, ComprarItemAsync).RequireAuthorization();
        rotas.MapPost(RotasEconomia.Vender, VenderItemAsync).RequireAuthorization();
        rotas.MapPost(RotasEconomia.Reparar, RepararItemAsync).RequireAuthorization();
        rotas.MapPost(RotasEconomia.RepararTudo, RepararTudoAsync).RequireAuthorization();
    }

    private static async Task<IResult> ObterSaldoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var id = ParaId(personagemId);
        var saldo = await servico.ObterSaldoAsync(id, c);
        return Results.Ok(new SaldoCoroasResponse(saldo));
    }

    private static async Task<IResult> ObterCatalogoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var itens = servico.ObterCatalogo();
        return Results.Ok(new RespostaCatalogoLoja(itens));
    }

    private static async Task<IResult> ComprarItemAsync(
        string personagemId,
        PedidoComprarItem pedido,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var id = ParaId(personagemId);
        var (sucesso, erro, resultado) = await servico.ComprarItemAsync(
            id,
            pedido.ItemId,
            pedido.Quantidade,
            c
        );

        if (!sucesso)
            return RespostasApi.Erro(StatusCodes.Status409Conflict, erro!);

        return Results.Ok(resultado);
    }

    private static async Task<IResult> VenderItemAsync(
        string personagemId,
        PedidoVenderItem pedido,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var id = ParaId(personagemId);
        var (sucesso, erro, resultado) = await servico.VenderItemAsync(id, pedido.InstanciaId, c);

        if (!sucesso)
            return RespostasApi.Erro(StatusCodes.Status409Conflict, erro!);

        return Results.Ok(resultado);
    }

    private static async Task<IResult> RepararItemAsync(
        string personagemId,
        PedidoRepararItem pedido,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var id = ParaId(personagemId);
        var (sucesso, erro, resultado) = await servico.RepararItemAsync(id, pedido.InstanciaId, c);

        if (!sucesso)
            return RespostasApi.Erro(StatusCodes.Status409Conflict, erro!);

        return Results.Ok(resultado);
    }

    private static async Task<IResult> RepararTudoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        ServicoDeEconomia servico,
        JogoDbContext db,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, c))
            return PersonagemNaoEncontrado();

        var id = ParaId(personagemId);
        var (sucesso, erro, custoTotal, saldoRestante, totalReparados) =
            await servico.RepararTudoAsync(id, c);

        if (!sucesso)
            return RespostasApi.Erro(StatusCodes.Status409Conflict, erro!);

        return Results.Ok(
            new
            {
                CustoTotal = custoTotal,
                SaldoAtualizado = saldoRestante,
                TotalReparados = totalReparados,
            }
        );
    }

    private static async Task<bool> PersonagemDaContaAsync(
        string personagemId,
        long contaId,
        JogoDbContext db,
        CancellationToken c
    )
    {
        if (
            !long.TryParse(
                personagemId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var id
            )
        )
            return false;

        return await db.Set<PersonagemEntidade>()
            .AnyAsync(p => p.Id == id && p.ContaId == contaId, c);
    }

    private static IResult PersonagemNaoEncontrado() =>
        RespostasApi.Erro(
            StatusCodes.Status404NotFound,
            CodigosErroEconomia.PersonagemNaoEncontrado
        );

    private static long ParaId(string valor) =>
        long.Parse(valor, NumberStyles.None, CultureInfo.InvariantCulture);
}
