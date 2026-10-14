using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class SkillsDoExemplar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillsDoExemplar",
                columns: table => new
                {
                    InstanciaId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModeloId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillIds = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillsDoExemplar", x => x.InstanciaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillsDoExemplar");
        }
    }
}
