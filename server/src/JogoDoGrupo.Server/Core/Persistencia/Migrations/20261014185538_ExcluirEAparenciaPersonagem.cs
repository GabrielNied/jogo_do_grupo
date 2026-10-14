using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ExcluirEAparenciaPersonagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cabelo",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "CorDeCabelo",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "TomDePele",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "NomesReservados",
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
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReservadoAte = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NomesReservados", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_NomesReservados_ContaId",
                table: "NomesReservados",
                column: "ContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NomesReservados_NomeNormalizado",
                table: "NomesReservados",
                column: "NomeNormalizado"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NomesReservados");

            migrationBuilder.DropColumn(name: "Cabelo", table: "Personagens");

            migrationBuilder.DropColumn(name: "CorDeCabelo", table: "Personagens");

            migrationBuilder.DropColumn(name: "TomDePele", table: "Personagens");
        }
    }
}
