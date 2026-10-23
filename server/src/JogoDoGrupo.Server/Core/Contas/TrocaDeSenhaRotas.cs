using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Troca de senha logado (#183). Grava o hash novo e renova o carimbo de segurança: todo token
/// emitido antes deixa de valer em HTTP e no tempo real, e quem trocou recebe um token novo.
/// </summary>
internal static class TrocaDeSenhaRotas
{
    public static IEndpointRouteBuilder MapearTrocaDeSenha(this IEndpointRouteBuilder rotas)
    {
        rotas
            .MapPost(RotasContas.TrocarSenha, TrocarSenhaAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitContas.PoliticaRateLimitContas);
        return rotas;
    }

    private static async Task<IResult> TrocarSenhaAsync(
        TrocarSenhaRequest requisicao,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        GeradorTokens tokens,
        ControleDeFalhasLogin controleFalhas,
        IVerificadorCarimboConta verificadorCarimbo,
        CancellationToken cancelamento
    )
    {
        var (conta, recusa) = await ContaComSenhaAtualAsync(
            usuario,
            requisicao.SenhaAtual,
            db,
            hasher,
            controleFalhas,
            cancelamento
        );
        if (conta == null)
        {
            return recusa!;
        }

        if (RegrasConta.ValidarSenha(requisicao.NovaSenha) is { } erro)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        conta.SenhaHash = hasher.HashPassword(conta, requisicao.NovaSenha);
        conta.RenovarCarimboSeguranca();
        await db.SaveChangesAsync(cancelamento);
        verificadorCarimbo.Invalidar(conta.Id);

        var (token, expiraEm) = tokens.Gerar(conta.Id, conta.CarimboSeguranca);
        return Results.Ok(
            new LoginResponse { Token = token, ExpiraEmUnix = expiraEm.ToUnixTimeSeconds() }
        );
    }

    /// <summary>
    /// Carrega a conta logada e confere a senha atual. Senha errada conta no
    /// <see cref="ControleDeFalhasLogin"/> (o mesmo do login) e devolve 400
    /// <c>SenhaAtualIncorreta</c>; bloqueado, 429. Sem conta, devolve a recusa.
    /// </summary>
    internal static async Task<(Conta? Conta, IResult? Recusa)> ContaComSenhaAtualAsync(
        ClaimsPrincipal usuario,
        string? senhaAtual,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        ControleDeFalhasLogin controleFalhas,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        var conta = await db.Set<Conta>().FirstOrDefaultAsync(c => c.Id == contaId, cancelamento);
        if (conta == null)
        {
            return (
                null,
                RespostasApi.Erro(StatusCodes.Status401Unauthorized, CodigosErro.NaoAutenticado)
            );
        }

        if (
            ContasRotas.RespostaSeBloqueado(controleFalhas, conta.UsuarioNormalizado) is
            { } bloqueado
        )
        {
            return (null, bloqueado);
        }

        // Senha atual vazia ou maior que o máximo nunca confere: recusa sem calcular hash.
        var confere =
            !string.IsNullOrEmpty(senhaAtual)
            && senhaAtual.Length <= RegrasConta.SenhaMaximo
            && hasher.VerifyHashedPassword(conta, conta.SenhaHash, senhaAtual)
                != PasswordVerificationResult.Failed;
        if (!confere)
        {
            await controleFalhas.RegistrarFalhaAsync(conta.UsuarioNormalizado, cancelamento);
            return (
                null,
                RespostasApi.Erro(StatusCodes.Status400BadRequest, CodigosErro.SenhaAtualIncorreta)
            );
        }

        controleFalhas.RegistrarSucesso(conta.UsuarioNormalizado);
        return (conta, null);
    }
}
