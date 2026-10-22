using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class PosicaoDoPersonagemPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosicoesDosPersonagens",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Area = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    RotacaoY = table.Column<float>(type: "real", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosicoesDosPersonagens", x => x.PersonagemId);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PosicoesDosPersonagens");
        }
    }
}
