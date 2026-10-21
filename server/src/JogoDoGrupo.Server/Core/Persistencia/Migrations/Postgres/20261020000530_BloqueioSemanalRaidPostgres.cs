using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BloqueioSemanalRaidPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueiosSemanaisRaid",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    ChaveSemana = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
                    Etapa = table.Column<int>(type: "integer", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_BloqueiosSemanaisRaid",
                        x => new
                        {
                            x.PersonagemId,
                            x.ChaveSemana,
                            x.Etapa,
                        }
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BloqueiosSemanaisRaid");
        }
    }
}
