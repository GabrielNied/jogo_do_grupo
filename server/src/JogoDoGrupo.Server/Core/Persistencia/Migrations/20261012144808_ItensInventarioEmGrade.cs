using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ItensInventarioEmGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Aba",
                table: "ItensInventario",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Casa",
                table: "ItensInventario",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Conteiner",
                table: "ItensInventario",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_PersonagemId_Conteiner_Aba_Casa",
                table: "ItensInventario",
                columns: new[] { "PersonagemId", "Conteiner", "Aba", "Casa" },
                unique: true,
                filter: "\"Casa\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItensInventario_PersonagemId_Conteiner_Aba_Casa",
                table: "ItensInventario"
            );

            migrationBuilder.DropColumn(name: "Aba", table: "ItensInventario");

            migrationBuilder.DropColumn(name: "Casa", table: "ItensInventario");

            migrationBuilder.DropColumn(name: "Conteiner", table: "ItensInventario");
        }
    }
}
