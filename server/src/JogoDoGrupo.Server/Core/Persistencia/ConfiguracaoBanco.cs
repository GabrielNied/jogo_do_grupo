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
        var conexao =
            configuracao.GetConnectionString("Jogo")
            ?? (
                provedor == ProvedorBanco.Postgres
                    ? configuracao.GetConnectionString("Postgres")
                    : null
            )
            ?? "Data Source=jogodogrupo.db";

        if (provedor == ProvedorBanco.Postgres)
        {
            servicos.AddDbContext<PostgresJogoDbContext>(opcoes =>
            {
                opcoes.UseNpgsql(conexao);
            });
            servicos.AddScoped<JogoDbContext>(sp => sp.GetRequiredService<PostgresJogoDbContext>());
        }
        else
        {
            servicos.AddDbContext<JogoDbContext>(opcoes =>
            {
                opcoes.UseSqlite(conexao);
            });
        }

        return servicos;
    }
}
