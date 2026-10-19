using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class MercadoDeOrdens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdensMercado",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Venda = table.Column<bool>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoUnitario = table.Column<long>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: false),
                    EncerradaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemEmCustodia = table.Column<bool>(type: "INTEGER", nullable: false),
                    EquipamentoJson = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensMercado", x => x.Id);
                    table.CheckConstraint(
                        "CK_OrdensMercado_PrecoUnitario",
                        "\"PrecoUnitario\" > 0"
                    );
                    table.ForeignKey(
                        name: "FK_OrdensMercado_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrdensMercado_Estado_ExpiraEm",
                table: "OrdensMercado",
                columns: new[] { "Estado", "ExpiraEm" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrdensMercado_ItemId_Venda_Estado_PrecoUnitario",
                table: "OrdensMercado",
                columns: new[] { "ItemId", "Venda", "Estado", "PrecoUnitario" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrdensMercado_PersonagemId_Estado",
                table: "OrdensMercado",
                columns: new[] { "PersonagemId", "Estado" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OrdensMercado");
        }
    }
}
