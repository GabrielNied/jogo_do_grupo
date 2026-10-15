using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ItensMateriaisDeFusao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaldosMateriaisFusao",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    EssenciasDungeon = table.Column<int>(type: "INTEGER", nullable: false),
                    EssenciasRaid = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosMateriaisFusao", x => x.PersonagemId);
                    table.CheckConstraint(
                        "CK_SaldosMateriaisFusao_Dungeon",
                        "\"EssenciasDungeon\" >= 0"
                    );
                    table.CheckConstraint("CK_SaldosMateriaisFusao_Raid", "\"EssenciasRaid\" >= 0");
                    table.ForeignKey(
                        name: "FK_SaldosMateriaisFusao_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SaldosMateriaisFusao");
        }
    }
}
