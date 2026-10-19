using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class BestiarioPersonagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BestiarioPersonagem",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonagemId = table.Column<long>(type: "INTEGER", nullable: false),
                    CriaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Derrotas = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimeiroRegistroEm = table.Column<long>(type: "INTEGER", nullable: false),
                    UltimoRegistroEm = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BestiarioPersonagem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BestiarioPersonagem_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_BestiarioPersonagem_PersonagemId",
                table: "BestiarioPersonagem",
                column: "PersonagemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BestiarioPersonagem_PersonagemId_CriaturaId",
                table: "BestiarioPersonagem",
                columns: new[] { "PersonagemId", "CriaturaId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BestiarioPersonagem");
        }
    }
}
