using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Persistencia;

/// <summary>
/// Único <see cref="DbContext"/> do servidor. Não lista as entidades: cada funcionalidade
/// declara as suas com uma classe <see cref="IEntityTypeConfiguration{TEntity}"/> na própria
/// pasta, e este contexto as descobre sozinho (como os módulos).
/// As funcionalidades recebem o contexto por injeção e acessam as tabelas com
/// <c>db.Set&lt;MinhaEntidade&gt;()</c>.
/// </summary>
public class JogoDbContext(DbContextOptions opcoes) : DbContext(opcoes)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JogoDbContext).Assembly);
    }
}

/// <summary>
/// Fábrica de tempo de design para o comando <c>dotnet ef</c> ao gerar migrações do SQLite.
/// </summary>
public sealed class JogoDbContextFactory
    : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<JogoDbContext>
{
    public JogoDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<JogoDbContext>()
            .UseSqlite(
                "Data Source=jogodogrupo.db",
                sqlite => sqlite.MigrationsAssembly(typeof(JogoDbContext).Assembly.GetName().Name)
            )
            .Options;
        return new JogoDbContext(opcoes);
    }
}
