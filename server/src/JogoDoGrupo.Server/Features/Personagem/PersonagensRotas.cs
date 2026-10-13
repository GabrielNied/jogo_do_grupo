using System.Data;
using System.Globalization;
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
    // Espera máxima pela trava do banco na criação (o client desiste em 10 s). O padrão da
    // conexão é 30 s; só a conexão desta requisição muda, e volta ao valor anterior no fim.
    private const int EsperaTravaSegundos = 5;

    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup(RotasPersonagem.Personagens).RequireAuthorization();
        grupo.MapGet("", ListarAsync);
        grupo.MapPost("", CriarAsync);
        grupo.MapGet("/{personagemId}/progresso", ProgressoAsync);
        grupo.MapPost("/{personagemId}/atributos", AtributosAsync);
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

        // O Microsoft.Data.Sqlite espera a trava pelo tempo do comando, que vem do
        // DefaultTimeout da conexão (inclusive no BEGIN e no COMMIT).
        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            return await CriarEmTransacaoAsync(requisicao, contaId, db, relogio, cancelamento);
        }
        catch (Exception e) when (e.EhBancoOcupado())
        {
            // Uma tentativa só: a transação já foi desfeita ao sair do método.
            log.LogWarning(
                "Banco ocupado por mais de {Espera} s ao criar personagem.",
                EsperaTravaSegundos
            );
            return RespostasApi.Erro(
                StatusCodes.Status503ServiceUnavailable,
                CodigosErro.ServidorOcupado,
                "Servidor ocupado. Tente de novo."
            );
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }

    /// <summary>
    /// Confere o limite e o nome e cria o personagem numa transação só.
    /// No SQLite, Serializable abre a transação com <c>BEGIN IMMEDIATE</c>: ela já começa com a
    /// trava de escrita, então dois pedidos simultâneos rodam um depois do outro (o segundo espera
    /// o primeiro terminar) e o segundo conta o personagem que o primeiro criou. Assim a conta
    /// nunca passa de <see cref="RegrasPersonagem.MaximoPorConta"/>.
    /// Isso vale só para o SQLite. No PostgreSQL, Serializable não trava na entrada: uma das
    /// transações concorrentes falha no commit com erro de serialização (40001) e precisaria ser
    /// tratada ou repetida. O Core reconhece essa falha em ErrosBanco.EhBancoOcupado,
    /// e esta rota responde 503 para permitir uma nova tentativa.
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

    private static async Task<IResult> ProgressoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        var personagem = await BuscarDaContaAsync(
            personagemId,
            usuario.ObterContaId(),
            db,
            cancelamento
        );
        return personagem == null
            ? PersonagemNaoEncontrado()
            : Results.Ok(personagem.ParaProgresso());
    }

    private static async Task<IResult> AtributosAsync(
        string personagemId,
        DistribuirAtributosRequest? requisicao,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ILogger<PersonagemModule> log,
        CancellationToken cancelamento
    )
    {
        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            return await GastarEmTransacaoAsync(
                personagemId,
                requisicao?.Pontos,
                usuario.ObterContaId(),
                db,
                cancelamento
            );
        }
        catch (Exception e) when (e.EhBancoOcupado())
        {
            log.LogWarning(
                "Banco ocupado por mais de {Espera} s ao gastar pontos de atributo.",
                EsperaTravaSegundos
            );
            return RespostasApi.Erro(
                StatusCodes.Status503ServiceUnavailable,
                CodigosErro.ServidorOcupado,
                "Servidor ocupado. Tente de novo."
            );
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }

    /// <summary>
    /// Gasta o pedido inteiro ou não gasta nada. Serializable no SQLite abre com
    /// <c>BEGIN IMMEDIATE</c>, então dois gastos simultâneos não usam os mesmos pontos.
    /// </summary>
    private static async Task<IResult> GastarEmTransacaoAsync(
        string personagemId,
        PontosDeAtributos? pontos,
        long contaId,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var personagem = await BuscarDaContaAsync(personagemId, contaId, db, cancelamento);
        if (personagem == null)
        {
            return PersonagemNaoEncontrado();
        }

        var erro = RegrasAtributos.ValidarDistribuicao(personagem.PontosLivres, pontos);
        if (erro == CodigosErroPersonagem.AtributoInvalido)
        {
            return RespostasApi.Erro(StatusCodes.Status400BadRequest, erro);
        }

        if (erro != null)
        {
            return RespostasApi.Erro(StatusCodes.Status409Conflict, erro);
        }

        var gasto = pontos!;
        personagem.PontosLivres -= (int)RegrasAtributos.Total(gasto);
        personagem.Forca += gasto.Forca;
        personagem.Agilidade += gasto.Agilidade;
        personagem.Intelecto += gasto.Intelecto;
        personagem.Vigor += gasto.Vigor;
        personagem.Espirito += gasto.Espirito;

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(personagem.ParaProgresso());
    }

    private static async Task<Personagem?> BuscarDaContaAsync(
        string personagemId,
        long contaId,
        JogoDbContext db,
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

        return await db.Set<Personagem>()
            .FirstOrDefaultAsync(p => p.Id == id && p.ContaId == contaId, cancelamento);
    }

    private static IResult PersonagemNaoEncontrado() =>
        RespostasApi.Erro(
            StatusCodes.Status404NotFound,
            CodigosErroPersonagem.PersonagemNaoEncontrado
        );
}
