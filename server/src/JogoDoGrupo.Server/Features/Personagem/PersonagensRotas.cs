using System.Data;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Listar e criar personagens (rota <see cref="RotasPersonagem.Personagens"/>, com token).
/// A conta vem sempre do token, nunca do corpo.
/// </summary>
internal static class PersonagensRotas
{
    // Quantas vezes a criação é tentada se o banco continuar ocupado depois do tempo de espera.
    private const int TentativasComBancoOcupado = 3;

    // SQLITE_BUSY e SQLITE_LOCKED: outra transação segura o banco.
    private const int SqliteOcupado = 5;
    private const int SqliteBloqueado = 6;

    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup(RotasPersonagem.Personagens).RequireAuthorization();
        grupo.MapGet("", ListarAsync);
        grupo.MapPost("", CriarAsync);
    }

    private static async Task<IResult> ListarAsync(
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        var personagens = await db.Set<Personagem>()
            .AsNoTracking()
            .Where(p => p.ContaId == contaId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancelamento);

        return Results.Ok(
            new ListaPersonagensResponse
            {
                Personagens = personagens.Select(p => p.ParaResumo()).ToArray(),
            }
        );
    }

    private static async Task<IResult> CriarAsync(
        CriarPersonagemRequest requisicao,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        TimeProvider relogio,
        ILogger<PersonagemModule> log,
        CancellationToken cancelamento
    )
    {
        var erro = RegrasPersonagem.ValidarNome(requisicao.Nome);
        if (erro != null)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        // 0 e qualquer número fora do enum são recusados.
        if (!Enum.IsDefined(requisicao.Raca))
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroPersonagem.RacaInvalida
            );
        }

        var contaId = usuario.ObterContaId();
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                return await CriarEmTransacaoAsync(requisicao, contaId, db, relogio, cancelamento);
            }
            catch (Exception e) when (EhBancoOcupado(e))
            {
                db.ChangeTracker.Clear();
                log.LogWarning(
                    "Banco ocupado ao criar personagem (tentativa {Tentativa} de {Total}).",
                    tentativa,
                    TentativasComBancoOcupado
                );
                if (tentativa >= TentativasComBancoOcupado)
                {
                    return RespostasApi.Erro(
                        StatusCodes.Status503ServiceUnavailable,
                        CodigosErro.ErroInterno,
                        "Servidor ocupado. Tente de novo."
                    );
                }
            }
        }
    }

    /// <summary>
    /// Confere o limite e o nome e cria o personagem numa transação só.
    /// Serializable no SQLite abre a transação com <c>BEGIN IMMEDIATE</c>: ela já começa com a
    /// trava de escrita, então dois pedidos simultâneos rodam um depois do outro (o segundo espera
    /// o primeiro terminar) e o segundo conta o personagem que o primeiro criou. Assim a conta
    /// nunca passa de <see cref="RegrasPersonagem.MaximoPorConta"/>.
    /// </summary>
    private static async Task<IResult> CriarEmTransacaoAsync(
        CriarPersonagemRequest requisicao,
        long contaId,
        JogoDbContext db,
        TimeProvider relogio,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var quantidade = await db.Set<Personagem>()
            .CountAsync(p => p.ContaId == contaId, cancelamento);
        if (quantidade >= RegrasPersonagem.MaximoPorConta)
        {
            return RespostasApi.Erro(
                StatusCodes.Status409Conflict,
                CodigosErroPersonagem.LimitePersonagens
            );
        }

        var normalizado = Personagem.Normalizar(requisicao.Nome);
        if (
            await db.Set<Personagem>().AnyAsync(p => p.NomeNormalizado == normalizado, cancelamento)
        )
        {
            return NomeJaExiste();
        }

        var personagem = new Personagem
        {
            ContaId = contaId,
            Nome = requisicao.Nome,
            NomeNormalizado = normalizado,
            Raca = requisicao.Raca,
            Nivel = Personagem.NivelInicial,
            CriadoEm = relogio.GetUtcNow().UtcDateTime,
        };
        db.Add(personagem);

        try
        {
            await db.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            // Proteção extra: o índice único recusou o nome (a transação é desfeita no dispose).
            return NomeJaExiste();
        }

        await transacao.CommitAsync(cancelamento);
        return Results.Created((string?)null, personagem.ParaResumo());
    }

    private static IResult NomeJaExiste() =>
        RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroPersonagem.NomeJaExiste);

    /// <summary>
    /// Indica se a falha foi o banco ocupado por outra transação depois do tempo de espera
    /// (pode vir direto, no BEGIN ou no COMMIT, ou dentro de um <see cref="DbUpdateException"/>).
    /// </summary>
    private static bool EhBancoOcupado(Exception erro)
    {
        for (var atual = erro; atual != null; atual = atual.InnerException)
        {
            if (atual is SqliteException { SqliteErrorCode: SqliteOcupado or SqliteBloqueado })
            {
                return true;
            }
        }

        return false;
    }
}
