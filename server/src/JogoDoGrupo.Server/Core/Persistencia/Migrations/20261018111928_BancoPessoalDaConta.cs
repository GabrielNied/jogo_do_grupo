using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class BancoPessoalDaConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadoBanco",
                columns: table => new
                {
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    Abas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoBanco", x => x.ContaId);
                }
            );

            migrationBuilder.CreateTable(
                name: "ItensBanco",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Aba = table.Column<int>(type: "INTEGER", nullable: true),
                    Casa = table.Column<int>(type: "INTEGER", nullable: true),
                    EquipamentoJson = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensBanco", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensBanco_ContaId",
                table: "ItensBanco",
                column: "ContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensBanco_ContaId_Aba_Casa",
                table: "ItensBanco",
                columns: new[] { "ContaId", "Aba", "Casa" },
                unique: true,
                filter: "\"Casa\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EstadoBanco");

            migrationBuilder.DropTable(name: "ItensBanco");
        }
    }
}
