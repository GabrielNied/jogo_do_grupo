using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JogoDoGrupo.Server.Tests.Features.Talentos;

/// <summary>
/// Quem já tinha pontos distribuídos nos cinco atributos (rota antiga) não perde nada: eles
/// voltam como pontos livres da árvore.
/// </summary>
public class MigracaoDosPontosTestes
{
    private const string MigracaoAnterior = "20261013031206_AtributosERecursosComNomesNovos";

    [Fact]
    public async Task Pontos_ja_distribuidos_voltam_como_pontos_livres()
    {
        var arquivo = Path.Combine(
            Path.GetTempPath(),
            $"jogodogrupo-migracao-{Guid.NewGuid():N}.db"
        );
        var opcoes = new DbContextOptionsBuilder<JogoDbContext>()
            .UseSqlite($"Data Source={arquivo}")
            .Options;
        try
        {
            await using (var db = new JogoDbContext(opcoes))
            {
                await db
                    .Database.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
                    .MigrateAsync(MigracaoAnterior);
                await db.Database.ExecuteSqlRawAsync(
                    "PRAGMA foreign_keys = OFF; "
                        + "INSERT INTO Personagens (Id, ContaId, Nome, NomeNormalizado, Raca, Nivel, XpAtual, PontosLivres, Forca, Agilidade, Intelecto, Vigor, Espirito, CriadoEm) "
                        + "VALUES (1, 1, 'Ana', 'ana', 1, 5, 0, 2, 3, 1, 0, 4, 2, '2026-10-13'), "
                        + "(2, 1, 'Bia', 'bia', 1, 1, 0, 0, 0, 0, 0, 0, 0, '2026-10-13');"
                );
            }

            await using (var db = new JogoDbContext(opcoes))
            {
                await db.Database.MigrateAsync();
            }

            await using var conexao = new SqliteConnection($"Data Source={arquivo}");
            await conexao.OpenAsync();
            var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT PontosLivres FROM Personagens ORDER BY Id";
            await using var leitor = await comando.ExecuteReaderAsync();
            Assert.True(await leitor.ReadAsync());
            Assert.Equal(2 + 3 + 1 + 0 + 4 + 2, leitor.GetInt32(0));
            Assert.True(await leitor.ReadAsync());
            Assert.Equal(0, leitor.GetInt32(0));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(arquivo);
        }
    }
}
