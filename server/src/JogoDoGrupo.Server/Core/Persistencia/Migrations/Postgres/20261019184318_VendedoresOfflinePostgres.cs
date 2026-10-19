using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class VendedoresOfflinePostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendedoresOffline",
                columns: table => new
                {
                    PersonagemId = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    NomeDono = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    PosicaoX = table.Column<float>(type: "real", nullable: false),
                    PosicaoZ = table.Column<float>(type: "real", nullable: false),
                    ContratadoEm = table.Column<long>(type: "bigint", nullable: false),
                    ExpiraEm = table.Column<long>(type: "bigint", nullable: false),
                    DiariasPagas = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendedoresOffline", x => x.PersonagemId);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_VendedoresOffline_Ativo_ExpiraEm",
                table: "VendedoresOffline",
                columns: new[] { "Ativo", "ExpiraEm" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VendedoresOffline");
        }
    }
}
