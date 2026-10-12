using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InfusaoECoroas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaldosCoroas",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Saldo = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosCoroas", x => x.PersonagemId);
                    table.CheckConstraint("CK_SaldosCoroas_Saldo", "\"Saldo\" >= 0");
                    table.ForeignKey(
                        name: "FK_SaldosCoroas_Personagens_PersonagemId",
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
            migrationBuilder.DropTable(name: "SaldosCoroas");
        }
    }
}
