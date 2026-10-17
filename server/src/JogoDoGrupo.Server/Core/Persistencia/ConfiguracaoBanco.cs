using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Persistencia;

public enum ProvedorBanco
{
    Sqlite,
    Postgres,
}

public static class ConfiguracaoBanco
{
    public const string ProvedorPadrao = "Sqlite";

    public static ProvedorBanco ObterProvedor(IConfiguration configuracao)
    {
        var nome = configuracao["Banco:Provedor"];
        if (string.IsNullOrWhiteSpace(nome))
        {
            return ProvedorBanco.Sqlite;
        }

        if (
            nome.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            || nome.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ProvedorBanco.Postgres;
        }

        return ProvedorBanco.Sqlite;
    }

    public static IServiceCollection AdicionarBancoDeDados(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        var provedor = ObterProvedor(configuracao);
        if (provedor == ProvedorBanco.Postgres)
        {
            var conexaoPostgres =
                configuracao.GetConnectionString("Jogo")
                ?? configuracao.GetConnectionString("Postgres")
                ?? configuracao["POSTGRES_CONEXAO"];

            if (string.IsNullOrWhiteSpace(conexaoPostgres))
            {
                throw new InvalidOperationException(
                    "Provedor de banco PostgreSQL selecionado, mas nenhuma string de conexão foi configurada. "
                        + "Defina a variável de ambiente ConnectionStrings__Jogo ou ConnectionStrings__Postgres."
                );
            }

            servicos.AddDbContext<PostgresJogoDbContext>(opcoes =>
            {
                opcoes.UseNpgsql(conexaoPostgres);
            });
            servicos.AddScoped<JogoDbContext>(sp => sp.GetRequiredService<PostgresJogoDbContext>());
        }
        else
        {
            var conexaoSqlite =
                configuracao.GetConnectionString("Jogo") ?? "Data Source=jogodogrupo.db";
            servicos.AddDbContext<JogoDbContext>(opcoes =>
            {
                opcoes.UseSqlite(conexaoSqlite);
            });
        }

        return servicos;
    }
}
