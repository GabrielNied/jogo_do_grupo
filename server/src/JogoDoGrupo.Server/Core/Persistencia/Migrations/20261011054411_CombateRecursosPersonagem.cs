using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CombateRecursosPersonagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecursosPersonagem",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Vida = table.Column<int>(type: "INTEGER", nullable: false),
                    Vigor = table.Column<int>(type: "INTEGER", nullable: false),
                    Energia = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoGastoVigorEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoGastoEnergiaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VigorContadoAte = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EnergiaContadaAte = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecursosPersonagem", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_RecursosPersonagem_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecursosPersonagem");
        }
    }
}
