using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BancoPessoalDaContaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadoBanco",
                columns: table => new
                {
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    Abas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
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
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Aba = table.Column<int>(type: "integer", nullable: true),
                    Casa = table.Column<int>(type: "integer", nullable: true),
                    EquipamentoJson = table.Column<string>(type: "text", nullable: true),
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
