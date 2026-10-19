using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BestiarioPersonagemPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BestiarioPersonagem",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    CriaturaId = table.Column<int>(type: "integer", nullable: false),
                    Derrotas = table.Column<int>(type: "integer", nullable: false),
                    PrimeiroRegistroEm = table.Column<long>(type: "bigint", nullable: false),
                    UltimoRegistroEm = table.Column<long>(type: "bigint", nullable: false),
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
