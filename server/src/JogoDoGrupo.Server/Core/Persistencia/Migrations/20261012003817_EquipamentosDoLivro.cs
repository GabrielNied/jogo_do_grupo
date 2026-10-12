using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class EquipamentosDoLivro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipamentoJson",
                table: "ItensInventario",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Posicao",
                table: "ItensInventario",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "EstadoEquipamentos",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    ConjuntoAtivo = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoEquipamentos", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_EstadoEquipamentos_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_PersonagemId_Posicao",
                table: "ItensInventario",
                columns: new[] { "PersonagemId", "Posicao" },
                unique: true,
                filter: "\"Posicao\" <> 0"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EstadoEquipamentos");

            migrationBuilder.DropIndex(
                name: "IX_ItensInventario_PersonagemId_Posicao",
                table: "ItensInventario"
            );

            migrationBuilder.DropColumn(name: "EquipamentoJson", table: "ItensInventario");

            migrationBuilder.DropColumn(name: "Posicao", table: "ItensInventario");
        }
    }
}
