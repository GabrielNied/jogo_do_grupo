using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class BloqueioSemanalRaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueiosSemanaisRaid",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChaveSemana = table.Column<string>(
                        type: "TEXT",
                        maxLength: 32,
                        nullable: false
                    ),
                    Etapa = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
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
