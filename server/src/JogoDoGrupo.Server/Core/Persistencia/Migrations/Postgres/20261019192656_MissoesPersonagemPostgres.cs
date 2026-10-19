using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MissoesPersonagemPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissoesPersonagem",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    MissaoId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Acompanhada = table.Column<bool>(type: "boolean", nullable: false),
                    ObjetivosJson = table.Column<string>(type: "text", nullable: false),
                    AceitaEm = table.Column<long>(type: "bigint", nullable: false),
                    ConcluidaEm = table.Column<long>(type: "bigint", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissoesPersonagem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissoesPersonagem_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MissoesPersonagem_PersonagemId_Estado",
                table: "MissoesPersonagem",
                columns: new[] { "PersonagemId", "Estado" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MissoesPersonagem_PersonagemId_MissaoId",
                table: "MissoesPersonagem",
                columns: new[] { "PersonagemId", "MissaoId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MissoesPersonagem");
        }
    }
}
