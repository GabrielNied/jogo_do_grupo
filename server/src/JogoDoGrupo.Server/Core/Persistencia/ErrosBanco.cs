using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Persistencia;

public static class ErrosBanco
{
    // SQLITE_CONSTRAINT_UNIQUE e SQLITE_CONSTRAINT_PRIMARYKEY.
    private const int SqliteUnico = 2067;
    private const int SqliteChavePrimaria = 1555;

    /// <summary>
    /// Indica se o <c>SaveChanges</c> falhou porque um índice único recusou o registro
    /// (ex.: duas requisições criando o mesmo nome ao mesmo tempo). Use depois de checar a
    /// duplicidade antes de salvar, para responder 409 também na corrida.
    /// Quando o banco mudar para PostgreSQL, só este método muda.
    /// </summary>
    public static bool EhViolacaoDeUnicidade(this DbUpdateException erro) =>
        erro.InnerException
            is SqliteException { SqliteExtendedErrorCode: SqliteUnico or SqliteChavePrimaria };
}
