using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class TalentosArvoreDeAtributos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Os pontos que o jogador já tinha distribuído nos cinco atributos voltam como
            // pontos livres da árvore (ele recompra nos nós). Nenhum ponto some; deve vir antes
            // de apagar as colunas.
            migrationBuilder.Sql(
                "UPDATE Personagens SET PontosLivres = PontosLivres + Forca + Agilidade + Intelecto + Vigor + Espirito;"
            );

            migrationBuilder.DropColumn(name: "Agilidade", table: "Personagens");

            migrationBuilder.DropColumn(name: "Espirito", table: "Personagens");

            migrationBuilder.DropColumn(name: "Forca", table: "Personagens");

            migrationBuilder.DropColumn(name: "Intelecto", table: "Personagens");

            migrationBuilder.DropColumn(name: "Vigor", table: "Personagens");

            migrationBuilder.CreateTable(
                name: "TalentosArvores",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Versao = table.Column<int>(type: "INTEGER", nullable: false),
                    PontosGastos = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentosArvores", x => x.PersonagemId);
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentosNos",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    NoId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentosNos", x => new { x.PersonagemId, x.NoId });
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A volta não recupera a distribuição antiga: as colunas voltam zeradas e os pontos
            // devolvidos ficam como pontos livres.
            migrationBuilder.DropTable(name: "TalentosArvores");

            migrationBuilder.DropTable(name: "TalentosNos");

            migrationBuilder.AddColumn<int>(
                name: "Agilidade",
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
                name: "Vigor",
                table: "Personagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );
        }
    }
}
