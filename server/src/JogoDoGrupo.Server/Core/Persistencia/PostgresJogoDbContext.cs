using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JogoDoGrupo.Server.Core.Persistencia;

/// <summary>
/// Contexto específico para PostgreSQL com migrações próprias em
/// <c>Core/Persistencia/Migrations/Postgres</c>. Herda de <see cref="JogoDbContext"/>
/// para reaproveitar todas as entidades e mapeamentos.
/// </summary>
public sealed class PostgresJogoDbContext(DbContextOptions<PostgresJogoDbContext> opcoes)
    : JogoDbContext(opcoes) { }

/// <summary>
/// Fábrica de tempo de design para o comando <c>dotnet ef</c> ao gerar migrações do PostgreSQL.
/// </summary>
public sealed class PostgresJogoDbContextFactory
    : IDesignTimeDbContextFactory<PostgresJogoDbContext>
{
    public PostgresJogoDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<PostgresJogoDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=jogodogrupo_design;Username=postgres;Password=postgres",
                npgsql =>
                    npgsql.MigrationsAssembly(typeof(PostgresJogoDbContext).Assembly.GetName().Name)
            )
            .Options;
        return new PostgresJogoDbContext(opcoes);
    }
}
