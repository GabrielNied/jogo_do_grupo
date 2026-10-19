using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class VendedoresOffline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendedoresOffline",
                columns: table => new
                {
                    PersonagemId = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeDono = table.Column<string>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    PosicaoX = table.Column<float>(type: "REAL", nullable: false),
                    PosicaoZ = table.Column<float>(type: "REAL", nullable: false),
                    ContratadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: false),
                    DiariasPagas = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
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
