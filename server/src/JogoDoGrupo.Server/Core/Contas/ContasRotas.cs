using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>Registro e login (rotas de <see cref="RotasContas"/>, sem token).</summary>
internal static class ContasRotas
{
    public static IServiceCollection AdicionarContas(this IServiceCollection servicos) =>
        servicos.AddSingleton<IPasswordHasher<Conta>, PasswordHasher<Conta>>();

    public static IEndpointRouteBuilder MapearContas(this IEndpointRouteBuilder rotas)
    {
        rotas.MapPost(RotasContas.Registrar, RegistrarAsync);
        rotas.MapPost(RotasContas.Login, LoginAsync);
        return rotas;
    }

    private static async Task<IResult> RegistrarAsync(
        RegistrarContaRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        TimeProvider relogio,
        CancellationToken cancelamento
    )
    {
        var erro =
            RegrasConta.ValidarUsuario(requisicao.Usuario)
            ?? RegrasConta.ValidarSenha(requisicao.Senha);
        if (erro != null)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        var normalizado = Conta.Normalizar(requisicao.Usuario);
        if (await db.Set<Conta>().AnyAsync(c => c.UsuarioNormalizado == normalizado, cancelamento))
        {
            return UsuarioJaExiste();
        }

        var conta = new Conta
        {
            Usuario = requisicao.Usuario,
            UsuarioNormalizado = normalizado,
            CriadaEm = relogio.GetUtcNow().UtcDateTime,
        };
        conta.SenhaHash = hasher.HashPassword(conta, requisicao.Senha);
        db.Add(conta);

        try
        {
            await db.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            // Outra requisição registrou o mesmo usuário entre a checagem e o insert.
            return UsuarioJaExiste();
        }

        return Results.StatusCode(StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        GeradorTokens tokens,
        CancellationToken cancelamento
    )
    {
        // Mesma resposta para qualquer falha, para não revelar se o usuário existe.
        var falha = RespostasApi.Erro(
            StatusCodes.Status401Unauthorized,
            CodigosErro.CredenciaisInvalidas
        );
        if (string.IsNullOrEmpty(requisicao.Usuario) || string.IsNullOrEmpty(requisicao.Senha))
        {
            return falha;
        }

        var normalizado = Conta.Normalizar(requisicao.Usuario);
        var conta = await db.Set<Conta>()
            .FirstOrDefaultAsync(c => c.UsuarioNormalizado == normalizado, cancelamento);
        if (conta == null)
        {
            return falha;
        }

        var resultado = hasher.VerifyHashedPassword(conta, conta.SenhaHash, requisicao.Senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return falha;
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            conta.SenhaHash = hasher.HashPassword(conta, requisicao.Senha);
            await db.SaveChangesAsync(cancelamento);
        }

        var (token, expiraEm) = tokens.Gerar(conta.Id);
        return Results.Ok(
            new LoginResponse { Token = token, ExpiraEmUnix = expiraEm.ToUnixTimeSeconds() }
        );
    }

    private static IResult UsuarioJaExiste() =>
        RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErro.UsuarioJaExiste);
}
