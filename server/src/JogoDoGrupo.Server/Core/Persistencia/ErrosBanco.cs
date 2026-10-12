using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Persistencia;

public static class ErrosBanco
{
    // SQLITE_BUSY e SQLITE_LOCKED: outra transação segura o banco.
    private const int SqliteOcupado = 5;
    private const int SqliteBloqueado = 6;

    /// <summary>
    /// Reconhece banco ocupado no SQLite e falha de serialização (SQLSTATE 40001)
    /// em provedores como PostgreSQL, inclusive dentro de exceções do EF Core.
    /// A transação deve ser desfeita antes de responder 503 ServidorOcupado.
    /// </summary>
    public static bool EhBancoOcupado(this Exception erro)
    {
        for (var atual = erro; atual != null; atual = atual.InnerException)
        {
            if (
                atual
                is SqliteException { SqliteErrorCode: SqliteOcupado or SqliteBloqueado }
                    or DbException { SqlState: "40001" }
            )
            {
                return true;
            }
        }

        return false;
    }

    // SQLITE_CONSTRAINT_UNIQUE e SQLITE_CONSTRAINT_PRIMARYKEY.
    private const int SqliteUnico = 2067;
    private const int SqliteChavePrimaria = 1555;

    // unique_violation no padrão SQL (PostgreSQL e outros).
    private const string SqlStateUnicidade = "23505";

    /// <summary>
    /// Indica se o <c>SaveChanges</c> falhou porque um índice único recusou o registro
    /// (ex.: duas requisições criando o mesmo nome ao mesmo tempo). Use depois de checar a
    /// duplicidade antes de salvar, para responder 409 também na corrida.
    /// Reconhece o SQLite e, pelo SQLSTATE 23505 de <see cref="DbException"/>, provedores como
    /// PostgreSQL (sem depender do pacote do provedor).
    /// </summary>
    public static bool EhViolacaoDeUnicidade(this DbUpdateException erro) =>
        erro.InnerException
            is SqliteException { SqliteExtendedErrorCode: SqliteUnico or SqliteChavePrimaria }
                or DbException { SqlState: SqlStateUnicidade };
}
