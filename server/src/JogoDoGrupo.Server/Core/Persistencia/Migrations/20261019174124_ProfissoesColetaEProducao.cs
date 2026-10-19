using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ProfissoesColetaEProducao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfissoesAprendidas",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Profissao = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ProfissoesAprendidas",
                        x => new { x.PersonagemId, x.Profissao }
                    );
                    table.ForeignKey(
                        name: "FK_ProfissoesAprendidas_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SaldosMateriaisProfissao",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SaldosMateriaisProfissao",
                        x => new { x.PersonagemId, x.MaterialId }
                    );
                    table.CheckConstraint(
                        "CK_SaldosMateriaisProfissao_Quantidade",
                        "\"Quantidade\" >= 0"
                    );
                    table.ForeignKey(
                        name: "FK_SaldosMateriaisProfissao_Personagens_PersonagemId",
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
            migrationBuilder.DropTable(name: "ProfissoesAprendidas");

            migrationBuilder.DropTable(name: "SaldosMateriaisProfissao");
        }
    }
}
