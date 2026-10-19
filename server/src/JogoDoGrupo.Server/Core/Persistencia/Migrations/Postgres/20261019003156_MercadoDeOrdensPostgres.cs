using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MercadoDeOrdensPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdensMercado",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Venda = table.Column<bool>(type: "boolean", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PrecoUnitario = table.Column<long>(type: "bigint", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    CriadaEm = table.Column<long>(type: "bigint", nullable: false),
                    ExpiraEm = table.Column<long>(type: "bigint", nullable: false),
                    EncerradaEm = table.Column<long>(type: "bigint", nullable: true),
                    ItemEmCustodia = table.Column<bool>(type: "boolean", nullable: false),
                    EquipamentoJson = table.Column<string>(type: "text", nullable: true),
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
