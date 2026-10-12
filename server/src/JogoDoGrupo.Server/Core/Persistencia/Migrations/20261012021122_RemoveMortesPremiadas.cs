using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMortesPremiadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MortesPremiadas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MortesPremiadas",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InimigoId = table.Column<long>(type: "INTEGER", nullable: false),
                    Sequencia = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MortesPremiadas", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MortesPremiadas_InimigoId_Sequencia",
                table: "MortesPremiadas",
                columns: new[] { "InimigoId", "Sequencia" },
                unique: true
            );
        }
    }
}
