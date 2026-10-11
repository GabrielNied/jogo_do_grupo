using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class PersonagemInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personagens",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    NomeNormalizado = table.Column<string>(
                        type: "TEXT",
                        maxLength: 16,
                        nullable: false
                    ),
                    Raca = table.Column<int>(type: "INTEGER", nullable: false),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personagens", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_ContaId",
                table: "Personagens",
                column: "ContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_NomeNormalizado",
                table: "Personagens",
                column: "NomeNormalizado",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Personagens");
        }
    }
}
