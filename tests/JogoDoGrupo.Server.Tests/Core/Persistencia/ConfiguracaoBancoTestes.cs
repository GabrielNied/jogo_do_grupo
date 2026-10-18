using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Persistencia;

public class ConfiguracaoBancoTestes
{
    [Theory]
    [InlineData(null, ProvedorBanco.Sqlite)]
    [InlineData("", ProvedorBanco.Sqlite)]
    [InlineData("   ", ProvedorBanco.Sqlite)]
    [InlineData("Sqlite", ProvedorBanco.Sqlite)]
    [InlineData("sqlite", ProvedorBanco.Sqlite)]
    [InlineData("SQLITE", ProvedorBanco.Sqlite)]
    [InlineData("Postgres", ProvedorBanco.Postgres)]
    [InlineData("postgres", ProvedorBanco.Postgres)]
    [InlineData("PostgreSQL", ProvedorBanco.Postgres)]
    [InlineData("POSTGRESQL", ProvedorBanco.Postgres)]
    public void ObterProvedor_reconhece_configuracao_com_padrao_sqlite(
        string? configurado,
        ProvedorBanco esperado
    )
    {
        var dict = new Dictionary<string, string?>();
        if (configurado != null)
        {
            dict["Banco:Provedor"] = configurado;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        var obtido = ConfiguracaoBanco.ObterProvedor(config);

        Assert.Equal(esperado, obtido);
    }

    [Fact]
    public void AdicionarBancoDeDados_registra_sqlite_por_padrao()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Jogo"] = "Data Source=teste_sqlite.db",
                }
            )
            .Build();

        var servicos = new ServiceCollection();
        servicos.AdicionarBancoDeDados(config);
        using var provedor = servicos.BuildServiceProvider();

        using var escopo = provedor.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        Assert.NotNull(db);
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
    }

    [Fact]
    public void AdicionarBancoDeDados_registra_postgres_quando_configurado()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Banco:Provedor"] = "Postgres",
                    ["ConnectionStrings:Jogo"] =
                        "Host=localhost;Database=jogodogrupo_teste;Username=postgres;Password=postgres",
                }
            )
            .Build();

        var servicos = new ServiceCollection();
        servicos.AdicionarBancoDeDados(config);
        using var provedor = servicos.BuildServiceProvider();

        using var escopo = provedor.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var dbPostgres = escopo.ServiceProvider.GetRequiredService<PostgresJogoDbContext>();

        Assert.NotNull(db);
        Assert.Same(db, dbPostgres);
        Assert.IsType<PostgresJogoDbContext>(db);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
    }

    [Fact]
    public void PostgresJogoDbContext_tem_migracoes_proprias()
    {
        var opcoes = new DbContextOptionsBuilder<PostgresJogoDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=jogodogrupo_teste;Username=postgres;Password=postgres"
            )
            .Options;

        using var db = new PostgresJogoDbContext(opcoes);
        var migracoes = db.Database.GetMigrations().ToList();

        Assert.NotEmpty(migracoes);
        Assert.Contains(migracoes, m => m.Contains("InicialPostgres"));
        Assert.Contains(migracoes, m => m.Contains("PetsColecaoDaConta"));
    }

    [Fact]
    public async Task Postgres_executa_migracao_do_zero_e_operacoes_quando_conexao_configurada()
    {
        var conexao =
            Environment.GetEnvironmentVariable("TESTE_POSTGRES_CONEXAO")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONEXAO");

        if (string.IsNullOrWhiteSpace(conexao))
        {
            // Pula quando executado localmente sem instância ativa do PostgreSQL.
            return;
        }

        var opcoes = new DbContextOptionsBuilder<PostgresJogoDbContext>()
            .UseNpgsql(conexao)
            .Options;

        await using var db = new PostgresJogoDbContext(opcoes);

        // Aplica as migrações do zero no banco de dados de teste
        await db.Database.MigrateAsync();

        var usuario = "teste" + Guid.NewGuid().ToString("N")[..8];
        var conta = new Conta
        {
            Usuario = usuario,
            UsuarioNormalizado = usuario.ToUpperInvariant(),
            SenhaHash = "hash-teste",
            CriadaEm = DateTime.UtcNow,
        };

        db.Set<Conta>().Add(conta);
        await db.SaveChangesAsync();

        Assert.True(conta.Id > 0, "Id da conta deve ter sido gerado pelo PostgreSQL.");

        // Valida unicidade e método EhViolacaoDeUnicidade
        var duplicada = new Conta
        {
            Usuario = usuario,
            UsuarioNormalizado = usuario.ToUpperInvariant(),
            SenhaHash = "outro-hash",
            CriadaEm = DateTime.UtcNow,
        };

        db.Set<Conta>().Add(duplicada);
        var excecao = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(
            excecao.EhViolacaoDeUnicidade(),
            "A violação de unicidade no Postgres deve ser reconhecida por EhViolacaoDeUnicidade()."
        );
    }
}
