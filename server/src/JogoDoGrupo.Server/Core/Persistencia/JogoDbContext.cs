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
