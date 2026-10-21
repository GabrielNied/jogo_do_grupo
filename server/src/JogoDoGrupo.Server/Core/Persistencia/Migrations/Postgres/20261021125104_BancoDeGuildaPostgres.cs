using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BancoDeGuildaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensBancoGuilda",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    GuildaId = table.Column<long>(type: "bigint", nullable: false),
                    Aba = table.Column<int>(type: "integer", nullable: false),
                    Casa = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    EquipamentoJson = table.Column<string>(type: "text", nullable: true),
                    DepositadoPorNome = table.Column<string>(type: "text", nullable: false),
                    DataDepositoUnix = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensBancoGuilda", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SaldosBancoGuilda",
                columns: table => new
                {
                    GuildaId = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Coroas = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosBancoGuilda", x => x.GuildaId);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensBancoGuilda_GuildaId",
                table: "ItensBancoGuilda",
                column: "GuildaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensBancoGuilda_GuildaId_Aba_Casa",
                table: "ItensBancoGuilda",
                columns: new[] { "GuildaId", "Aba", "Casa" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ItensBancoGuilda");

            migrationBuilder.DropTable(name: "SaldosBancoGuilda");
        }
    }
}
