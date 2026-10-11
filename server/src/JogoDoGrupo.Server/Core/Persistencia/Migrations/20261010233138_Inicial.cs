using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsuarioNormalizado = table.Column<string>(
                        type: "TEXT",
                        maxLength: 20,
                        nullable: false
                    ),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Contas_UsuarioNormalizado",
                table: "Contas",
                column: "UsuarioNormalizado",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Contas");
        }
    }
}
