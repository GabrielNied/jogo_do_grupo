using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Persistencia;

/// <summary>
/// A migração dos nomes novos (#622) só renomeia colunas: quem já tinha pontos e recursos
/// continua com os mesmos valores (Destreza vira Agilidade, Vitalidade vira Vigor, vigor vira
/// stamina e energia vira mana).
/// </summary>
public class MigracaoDosNomesNovosTestes
{
    private const string MigracaoAnterior = "20261012144808_ItensInventarioEmGrade";
    private const string MigracaoNova = "20261013031206_AtributosERecursosComNomesNovos";

    private const int Forca = 1;
    private const int DestrezaAntiga = 2;
    private const int Intelecto = 3;
    private const int VitalidadeAntiga = 4;
    private const int Espirito = 5;
    private const int VigorAntigo = 77;
    private const int EnergiaAntiga = 66;

    [Fact]
    public void Pontos_e_recursos_guardados_continuam_depois_de_renomear_as_colunas()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"jogodogrupo-nomes-{Guid.NewGuid():N}.db");
        try
        {
            var opcoes = new DbContextOptionsBuilder<JogoDbContext>()
                .UseSqlite($"Data Source={arquivo};Foreign Keys=False;Pooling=False")
                .Options;
            using var db = new JogoDbContext(opcoes);
            var migrador = db.GetService<IMigrator>();

            migrador.Migrate(MigracaoAnterior);
            db.Database.ExecuteSql(
                $"""
                INSERT INTO "Personagens"
                    ("Id", "ContaId", "CriadoEm", "Nome", "NomeNormalizado", "Raca", "Nivel",
                     "XpAtual", "PontosLivres", "Forca", "Destreza", "Intelecto", "Vitalidade",
                     "Espirito")
                VALUES
                    (1, 1, '2026-10-12 00:00:00', 'Ana', 'ANA', 1, 3, 0, 0, {Forca},
                     {DestrezaAntiga}, {Intelecto}, {VitalidadeAntiga}, {Espirito})
                """
            );
            db.Database.ExecuteSql(
                $"""
                INSERT INTO "RecursosPersonagem"
                    ("PersonagemId", "Vida", "Vigor", "Energia", "UltimoGastoVigorEm",
                     "UltimoGastoEnergiaEm", "VigorContadoAte", "EnergiaContadaAte")
                VALUES
                    (1, 500, {VigorAntigo}, {EnergiaAntiga}, '2026-10-12 01:00:00',
                     '2026-10-12 02:00:00', '2026-10-12 03:00:00', '2026-10-12 04:00:00')
                """
            );

            migrador.Migrate(MigracaoNova);

            var pontos = db
                .Database.SqlQuery<int>(
                    $"""
                    SELECT "Forca" + "Agilidade" * 10 + "Intelecto" * 100 + "Vigor" * 1000
                        + "Espirito" * 10000 AS "Value"
                    FROM "Personagens" WHERE "Id" = 1
                    """
                )
                .Single();
            Assert.Equal(
                Forca
                    + DestrezaAntiga * 10
                    + Intelecto * 100
                    + VitalidadeAntiga * 1000
                    + Espirito * 10000,
                pontos
            );

            var recursos = db
                .Database.SqlQuery<string>(
                    $"""
                    SELECT "Stamina" || '|' || "Mana" || '|' || "UltimoGastoStaminaEm" || '|'
                        || "UltimoGastoManaEm" || '|' || "StaminaContadaAte" || '|'
                        || "ManaContadaAte" AS "Value"
                    FROM "RecursosPersonagem" WHERE "PersonagemId" = 1
                    """
                )
                .Single();
            Assert.Equal(
                $"{VigorAntigo}|{EnergiaAntiga}|2026-10-12 01:00:00|2026-10-12 02:00:00"
                    + "|2026-10-12 03:00:00|2026-10-12 04:00:00",
                recursos
            );
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
