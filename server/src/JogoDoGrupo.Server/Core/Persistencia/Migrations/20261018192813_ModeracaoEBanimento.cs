using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ModeracaoEBanimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanidaAte",
                table: "Contas",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "EhGm",
                table: "Contas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "MotivoBanimento",
                table: "Contas",
                type: "TEXT",
                maxLength: 256,
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "AuditoriaGm",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GmContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    GmPersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Alvo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Detalhes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExecutadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaGm", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AutorContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    AutorPersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    AlvoContaId = table.Column<long>(type: "INTEGER", nullable: true),
                    AlvoPersonagemId = table.Column<long>(type: "INTEGER", nullable: true),
                    AlvoNome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Detalhes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MetadadosContexto = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: false
                    ),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaGm_ExecutadaEm",
                table: "AuditoriaGm",
                column: "ExecutadaEm"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaGm_GmContaId",
                table: "AuditoriaGm",
                column: "GmContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AlvoPersonagemId",
                table: "Denuncias",
                column: "AlvoPersonagemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AutorContaId",
                table: "Denuncias",
                column: "AutorContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_CriadaEm",
                table: "Denuncias",
                column: "CriadaEm"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditoriaGm");

            migrationBuilder.DropTable(name: "Denuncias");

            migrationBuilder.DropColumn(name: "BanidaAte", table: "Contas");

            migrationBuilder.DropColumn(name: "EhGm", table: "Contas");

            migrationBuilder.DropColumn(name: "MotivoBanimento", table: "Contas");
        }
    }
}
