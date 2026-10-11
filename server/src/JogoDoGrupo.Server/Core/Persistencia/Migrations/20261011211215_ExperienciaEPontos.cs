using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ExperienciaEPontos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Destreza",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Espirito",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Forca",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Intelecto",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "PontosLivres",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Vitalidade",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<long>(
                name: "XpAtual",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L
            );

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MortesPremiadas");

            migrationBuilder.DropColumn(name: "Destreza", table: "Personagens");

            migrationBuilder.DropColumn(name: "Espirito", table: "Personagens");

            migrationBuilder.DropColumn(name: "Forca", table: "Personagens");

            migrationBuilder.DropColumn(name: "Intelecto", table: "Personagens");

            migrationBuilder.DropColumn(name: "PontosLivres", table: "Personagens");

            migrationBuilder.DropColumn(name: "Vitalidade", table: "Personagens");

            migrationBuilder.DropColumn(name: "XpAtual", table: "Personagens");
        }
    }
}
