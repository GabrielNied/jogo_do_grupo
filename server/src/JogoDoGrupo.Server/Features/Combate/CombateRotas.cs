using System.Data;
using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Recursos do personagem (rota <see cref="RotasCombate.Recursos"/>, com token). A conta vem
/// sempre do token; personagem inexistente e de outra conta respondem o mesmo 404.
/// </summary>
internal static class CombateRotas
{
    // Mesma espera pela trava do banco de Itens (o client desiste em 10 s).
    private const int EsperaTravaSegundos = 5;
    private const int SqliteOcupado = 5;
    private const int SqliteBloqueado = 6;

    public static void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapGet(RotasCombate.Recursos, RecursosAsync).RequireAuthorization();

    private static async Task<IResult> RecursosAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        IBonusDeAtributos bonus,
        IPontosDistribuidos pontos,
        TimeProvider relogio,
        ILogger<CombateModule> log,
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

        var atributos = CalculadoraRecursos.AtributosFinais(
            await bonus.EquipamentosAsync(id, cancelamento),
            await bonus.EfeitosAsync(id, cancelamento),
            await pontos.ObterAsync(id, cancelamento)
        );

        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            var resposta = await AtualizarEmTransacaoAsync(
                id,
                atributos,
                db,
                relogio.GetUtcNow().UtcDateTime,
                cancelamento
            );
            return Results.Ok(resposta);
        }
        catch (Exception e) when (EhBancoOcupado(e))
        {
            log.LogWarning(
                "Banco ocupado por mais de {Espera} s ao ler recursos.",
                EsperaTravaSegundos
            );
            return RespostasApi.Erro(
                StatusCodes.Status500InternalServerError,
                CodigosErro.ErroInterno,
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
    /// Lê, regenera e grava numa transação só. No SQLite, Serializable abre com
    /// <c>BEGIN IMMEDIATE</c>: consultas simultâneas rodam uma depois da outra e a regeneração
    /// nunca é paga duas vezes (mesmo cuidado de Itens; no PostgreSQL seria outro, issue #28).
    /// </summary>
    private static async Task<RecursosResponse> AtualizarEmTransacaoAsync(
        long personagemId,
        AtributosCombate atributos,
        JogoDbContext db,
        DateTime agora,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var recursos = await db.Set<RecursosPersonagem>()
            .FirstOrDefaultAsync(r => r.PersonagemId == personagemId, cancelamento);
        if (recursos == null)
        {
            recursos = CalculadoraRecursos.Novo(personagemId, atributos, agora);
            db.Set<RecursosPersonagem>().Add(recursos);
        }
        else
        {
            CalculadoraRecursos.Atualizar(recursos, atributos, agora);
        }

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        return new RecursosResponse
        {
            VidaAtual = recursos.Vida,
            VidaMaxima = RegrasRecursos.VidaMaxima(atributos.Vitalidade),
            VigorAtual = recursos.Vigor,
            VigorMaximo = RegrasRecursos.VigorMaximo(),
            EnergiaAtual = recursos.Energia,
            EnergiaMaxima = RegrasRecursos.EnergiaMaxima(atributos.Espirito),
            Atributos = atributos,
        };
    }

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
