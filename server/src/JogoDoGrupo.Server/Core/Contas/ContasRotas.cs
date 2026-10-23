using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Registro e login (rotas de <see cref="RotasContas"/>, sem token); a troca de senha fica em
/// <see cref="TrocaDeSenhaRotas"/>.
/// </summary>
internal static class ContasRotas
{
    // Hash fixo usado no login de conta inexistente, gerado uma vez com o hasher padrão.
    internal static readonly Conta ContaFicticia = new();
    internal static readonly Lazy<string> HashFicticio = new(() =>
        new PasswordHasher<Conta>().HashPassword(ContaFicticia, Guid.NewGuid().ToString())
    );

    public static IServiceCollection AdicionarContas(this IServiceCollection servicos) =>
        servicos
            .AddSingleton<IPasswordHasher<Conta>, PasswordHasher<Conta>>()
            .AddSingleton<ControleDeFalhasLogin>()
            .AddSingleton<IVerificadorCarimboConta, VerificadorCarimboConta>();

    public static IEndpointRouteBuilder MapearContas(this IEndpointRouteBuilder rotas)
    {
        rotas
            .MapPost(RotasContas.Registrar, RegistrarAsync)
            .RequireRateLimiting(RateLimitContas.PoliticaRateLimitContas);
        rotas
            .MapPost(RotasContas.Login, LoginAsync)
            .RequireRateLimiting(RateLimitContas.PoliticaRateLimitContas);
        rotas.MapearTrocaDeSenha();
        rotas.MapearRecuperacaoDeConta();
        return rotas;
    }

    /// <summary>
    /// Responde 429 se o usuário está bloqueado por falhas seguidas (login ou senha atual errada).
    /// </summary>
    internal static IResult? RespostaSeBloqueado(
        ControleDeFalhasLogin controleFalhas,
        string usuarioNormalizado
    )
    {
        if (!controleFalhas.EstaBloqueado(usuarioNormalizado, out var tempoRestante))
        {
            return null;
        }

        var segundos = Math.Max(1, (int)Math.Ceiling(tempoRestante.TotalSeconds));
        return RespostasApi.Erro(
            StatusCodes.Status429TooManyRequests,
            CodigosErro.MuitasTentativas,
            $"Muitas tentativas falhas. Aguarde {segundos} segundo(s) antes de tentar novamente."
        );
    }

    private static async Task<IResult> RegistrarAsync(
        RegistrarContaRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        TimeProvider relogio,
        CancellationToken cancelamento
    )
    {
        var temEmail = !string.IsNullOrWhiteSpace(requisicao.Email);
        var erro =
            RegrasConta.ValidarUsuario(requisicao.Usuario)
            ?? RegrasConta.ValidarSenha(requisicao.Senha)
            ?? (temEmail ? RegrasConta.ValidarEmail(requisicao.Email.Trim()) : null);
        if (erro != null)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        var normalizado = Conta.Normalizar(requisicao.Usuario);
        if (await db.Set<Conta>().AnyAsync(c => c.UsuarioNormalizado == normalizado, cancelamento))
        {
            return UsuarioJaExiste();
        }

        if (
            temEmail
            && await RecuperacaoDeContaRotas.EmailEmUsoAsync(db, requisicao.Email, 0, cancelamento)
        )
        {
            return RecuperacaoDeContaRotas.EmailJaEmUso();
        }

        var conta = new Conta
        {
            Usuario = requisicao.Usuario,
            UsuarioNormalizado = normalizado,
            CriadaEm = relogio.GetUtcNow().UtcDateTime,
        };
        conta.SenhaHash = hasher.HashPassword(conta, requisicao.Senha);
        conta.DefinirEmail(temEmail ? requisicao.Email : null);
        db.Add(conta);

        try
        {
            await db.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            // Outra requisição registrou o mesmo usuário (ou e-mail) entre a checagem e o insert.
            return
                temEmail
                && !await db.Set<Conta>()
                    .AnyAsync(c => c.UsuarioNormalizado == normalizado, cancelamento)
                ? RecuperacaoDeContaRotas.EmailJaEmUso()
                : UsuarioJaExiste();
        }

        return Results.StatusCode(StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        GeradorTokens tokens,
        ControleDeFalhasLogin controleFalhas,
        CancellationToken cancelamento
    )
    {
        // Mesma resposta para qualquer falha, para não revelar se o usuário existe.
        var falha = RespostasApi.Erro(
            StatusCodes.Status401Unauthorized,
            CodigosErro.CredenciaisInvalidas
        );
        // Valores vazios ou maiores que o máximo nunca existem: responde sem consultar o banco
        // nem calcular hash (evita gastar CPU com senhas enormes).
        if (
            string.IsNullOrEmpty(requisicao.Usuario)
            || string.IsNullOrEmpty(requisicao.Senha)
            || requisicao.Usuario.Length > RegrasConta.UsuarioMaximo
            || requisicao.Senha.Length > RegrasConta.SenhaMaximo
        )
        {
            return falha;
        }

        var normalizado = Conta.Normalizar(requisicao.Usuario);

        if (RespostaSeBloqueado(controleFalhas, normalizado) is { } bloqueado)
        {
            return bloqueado;
        }

        var conta = await db.Set<Conta>()
            .FirstOrDefaultAsync(c => c.UsuarioNormalizado == normalizado, cancelamento);
        if (conta == null)
        {
            // Calcula um hash mesmo assim, para o tempo de resposta não revelar se o usuário existe.
            hasher.VerifyHashedPassword(ContaFicticia, HashFicticio.Value, requisicao.Senha);
            await controleFalhas.RegistrarFalhaAsync(normalizado, cancelamento);
            return falha;
        }

        var resultado = hasher.VerifyHashedPassword(conta, conta.SenhaHash, requisicao.Senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            await controleFalhas.RegistrarFalhaAsync(normalizado, cancelamento);
            return falha;
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            conta.SenhaHash = hasher.HashPassword(conta, requisicao.Senha);
            await db.SaveChangesAsync(cancelamento);
        }

        controleFalhas.RegistrarSucesso(normalizado);

        var agora = DateTime.UtcNow;
        if (conta.BanidaAte is { } banidaAte && banidaAte > agora)
        {
            var motivo = string.IsNullOrWhiteSpace(conta.MotivoBanimento)
                ? "Violação das regras do jogo."
                : conta.MotivoBanimento;
            return RespostasApi.Erro(
                StatusCodes.Status403Forbidden,
                CodigosErro.ContaBanida,
                $"Conta suspensa até {banidaAte:yyyy-MM-dd HH:mm:ss} UTC. Motivo: {motivo}"
            );
        }

        var (token, expiraEm) = tokens.Gerar(conta.Id, conta.CarimboSeguranca);
        return Results.Ok(
            new LoginResponse { Token = token, ExpiraEmUnix = expiraEm.ToUnixTimeSeconds() }
        );
    }

    private static IResult UsuarioJaExiste() =>
        RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErro.UsuarioJaExiste);
}
