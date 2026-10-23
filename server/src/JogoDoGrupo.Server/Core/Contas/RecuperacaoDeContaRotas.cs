using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Email;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// E-mail de recuperação na conta e redefinição de senha por código mandado ao e-mail (#1167).
/// O pedido responde igual exista ou não a conta; só o hash do código fica no banco.
/// </summary>
internal static class RecuperacaoDeContaRotas
{
    public static IServiceCollection AdicionarRecuperacaoDeConta(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        var limitePorConta =
            configuracao.GetValue<int?>("Contas:Recuperacao:PedidosPorHoraPorConta")
            ?? RecuperacaoDeConta.PedidosPorHoraPorContaPadrao;
        return servicos.AddSingleton(sp => new ControleDePedidosRecuperacao(
            sp.GetRequiredService<TimeProvider>(),
            limitePorConta
        ));
    }

    public static IEndpointRouteBuilder MapearRecuperacaoDeConta(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet(RotasContas.Dados, DadosAsync).RequireAuthorization();
        rotas
            .MapPost(RotasContas.DefinirEmail, DefinirEmailAsync)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitContas.PoliticaRateLimitContas);
        rotas
            .MapPost(RotasContas.PedirCodigoRecuperacao, PedirCodigoAsync)
            .RequireRateLimiting(RateLimitContas.PoliticaPedidosRecuperacao);
        rotas
            .MapPost(RotasContas.RedefinirSenha, RedefinirSenhaAsync)
            .RequireRateLimiting(RateLimitContas.PoliticaRedefinicaoSenha);
        return rotas;
    }

    private static async Task<IResult> DadosAsync(
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        var dados = await db.Set<Conta>()
            .AsNoTracking()
            .Where(c => c.Id == contaId)
            .Select(c => new DadosContaResponse { Usuario = c.Usuario, Email = c.Email ?? "" })
            .FirstOrDefaultAsync(cancelamento);
        return dados == null
            ? RespostasApi.Erro(StatusCodes.Status401Unauthorized, CodigosErro.NaoAutenticado)
            : Results.Ok(dados);
    }

    private static async Task<IResult> DefinirEmailAsync(
        DefinirEmailRequest requisicao,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        ControleDeFalhasLogin controleFalhas,
        CancellationToken cancelamento
    )
    {
        var (conta, recusa) = await TrocaDeSenhaRotas.ContaComSenhaAtualAsync(
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

        var tirar = string.IsNullOrWhiteSpace(requisicao.Email);
        if (!tirar && RegrasConta.ValidarEmail(requisicao.Email.Trim()) is { } erro)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        if (!tirar && await EmailEmUsoAsync(db, requisicao.Email, conta.Id, cancelamento))
        {
            return EmailJaEmUso();
        }

        conta.DefinirEmail(tirar ? null : requisicao.Email);
        // Um código pedido para o e-mail antigo não vale para o novo.
        conta.EncerrarRecuperacao();
        try
        {
            await db.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            return EmailJaEmUso();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> PedirCodigoAsync(
        PedirCodigoRecuperacaoRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        ControleDePedidosRecuperacao controlePedidos,
        FilaDeEmails emails,
        TimeProvider relogio,
        CancellationToken cancelamento
    )
    {
        if (RegrasConta.ValidarEmail(requisicao.Email?.Trim()) is { } erro)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        // A mesma resposta em todos os caminhos, para não revelar se o e-mail tem conta.
        var aceito = Results.StatusCode(StatusCodes.Status202Accepted);
        var codigo = RecuperacaoDeConta.GerarCodigo();
        var normalizado = Conta.NormalizarEmail(requisicao.Email!);
        var conta = await db.Set<Conta>()
            .FirstOrDefaultAsync(c => c.EmailNormalizado == normalizado, cancelamento);
        if (conta == null || !controlePedidos.TentarRegistrar(conta.Id))
        {
            // Calcula um hash mesmo assim, para o tempo de resposta não revelar a conta.
            hasher.HashPassword(ContasRotas.ContaFicticia, codigo);
            return aceito;
        }

        conta.IniciarRecuperacao(
            hasher.HashPassword(conta, codigo),
            relogio.GetUtcNow().UtcDateTime + RecuperacaoDeConta.ValidadeCodigo
        );
        await db.SaveChangesAsync(cancelamento);
        emails.Enfileirar(
            new MensagemEmail(
                conta.Email!,
                RecuperacaoDeConta.AssuntoEmail,
                RecuperacaoDeConta.TextoEmail(conta.Usuario, codigo)
            )
        );
        return aceito;
    }

    private static async Task<IResult> RedefinirSenhaAsync(
        RedefinirSenhaRequest requisicao,
        JogoDbContext db,
        IPasswordHasher<Conta> hasher,
        IVerificadorCarimboConta verificadorCarimbo,
        ControleDeFalhasLogin controleFalhas,
        ServidorTempoReal tempoReal,
        TimeProvider relogio,
        CancellationToken cancelamento
    )
    {
        var invalido = RespostasApi.Erro(
            StatusCodes.Status400BadRequest,
            CodigosErro.CodigoRecuperacaoInvalido
        );
        if (
            RegrasConta.ValidarEmail(requisicao.Email?.Trim()) != null
            || !RegrasConta.CodigoRecuperacaoNoFormato(requisicao.Codigo)
        )
        {
            return invalido;
        }

        if (RegrasConta.ValidarSenha(requisicao.NovaSenha) is { } erroSenha)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erroSenha);
        }

        var normalizado = Conta.NormalizarEmail(requisicao.Email!);
        var conta = await db.Set<Conta>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmailNormalizado == normalizado, cancelamento);
        if (conta == null || !conta.RecuperacaoAtiva(relogio.GetUtcNow().UtcDateTime))
        {
            hasher.VerifyHashedPassword(
                ContasRotas.ContaFicticia,
                ContasRotas.HashFicticio.Value,
                requisicao.Codigo
            );
            return invalido;
        }

        // As escritas abaixo só valem se o código lido ainda for o ativo: um pedido novo ou outra
        // redefinição no meio do caminho fazem esta falhar (uso único mesmo em corrida).
        var hashLido = conta.CodigoRecuperacaoHash!;
        var doCodigoLido = db.Set<Conta>()
            .Where(c => c.Id == conta.Id && c.CodigoRecuperacaoHash == hashLido);

        if (
            hasher.VerifyHashedPassword(conta, hashLido, requisicao.Codigo)
            == PasswordVerificationResult.Failed
        )
        {
            await doCodigoLido.ExecuteUpdateAsync(
                s =>
                    s.SetProperty(
                        c => c.CodigoRecuperacaoTentativas,
                        c => c.CodigoRecuperacaoTentativas + 1
                    ),
                cancelamento
            );
            return invalido;
        }

        var senhaHash = hasher.HashPassword(conta, requisicao.NovaSenha);
        conta.RenovarCarimboSeguranca();
        var carimbo = conta.CarimboSeguranca;
        var alteradas = await doCodigoLido.ExecuteUpdateAsync(
            s =>
                s.SetProperty(c => c.SenhaHash, senhaHash)
                    .SetProperty(c => c.CarimboSeguranca, carimbo)
                    .SetProperty(c => c.CodigoRecuperacaoHash, (string?)null)
                    .SetProperty(c => c.CodigoRecuperacaoExpiraEm, (DateTime?)null)
                    .SetProperty(c => c.CodigoRecuperacaoTentativas, 0),
            cancelamento
        );
        if (alteradas == 0)
        {
            return invalido;
        }

        verificadorCarimbo.Invalidar(conta.Id);
        // Quem redefiniu provou ter o e-mail: libera o login bloqueado por falhas.
        controleFalhas.RegistrarSucesso(conta.UsuarioNormalizado);
        // Sessões antigas caem também no tempo real (outro PC dentro do mundo).
        var contaId = conta.Id;
        tempoReal.NoLaco(() => tempoReal.DesconectarConta(contaId));
        return Results.NoContent();
    }

    /// <summary>Se outra conta (diferente de <paramref name="contaId"/>) já usa o e-mail.</summary>
    internal static Task<bool> EmailEmUsoAsync(
        JogoDbContext db,
        string email,
        long contaId,
        CancellationToken cancelamento
    )
    {
        var normalizado = Conta.NormalizarEmail(email);
        return db.Set<Conta>()
            .AnyAsync(c => c.EmailNormalizado == normalizado && c.Id != contaId, cancelamento);
    }

    internal static IResult EmailJaEmUso() =>
        RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErro.EmailJaEmUso);
}
