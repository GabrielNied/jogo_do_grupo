using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class MontariasColecaoDaConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MontariasDaConta",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    TipoMontaria = table.Column<int>(type: "INTEGER", nullable: false),
                    DesbloqueadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MontariasDaConta", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MontariasDaConta_ContaId_TipoMontaria",
                table: "MontariasDaConta",
                columns: new[] { "ContaId", "TipoMontaria" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MontariasDaConta");
        }
    }
}
