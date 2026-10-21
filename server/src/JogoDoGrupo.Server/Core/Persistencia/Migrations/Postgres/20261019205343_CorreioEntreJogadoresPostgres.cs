using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CorreioEntreJogadoresPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartasCorreio",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    DestinatarioId = table.Column<long>(type: "bigint", nullable: false),
                    RemetenteId = table.Column<long>(type: "bigint", nullable: true),
                    RemetenteNome = table.Column<string>(
                        type: "character varying(40)",
                        maxLength: 40,
                        nullable: false
                    ),
                    Assunto = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Mensagem = table.Column<string>(
                        type: "character varying(1500)",
                        maxLength: 1500,
                        nullable: false
                    ),
                    Coroas = table.Column<long>(type: "bigint", nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false),
                    AnexosColetados = table.Column<bool>(type: "boolean", nullable: false),
                    Devolvida = table.Column<bool>(type: "boolean", nullable: false),
                    DoSistema = table.Column<bool>(type: "boolean", nullable: false),
                    EnviadaEm = table.Column<long>(type: "bigint", nullable: false),
                    ExpiraEm = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartasCorreio", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AnexosItensCorreio",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    CartaId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    EquipamentoJson = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnexosItensCorreio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnexosItensCorreio_CartasCorreio_CartaId",
                        column: x => x.CartaId,
                        principalTable: "CartasCorreio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AnexosItensCorreio_CartaId",
                table: "AnexosItensCorreio",
                column: "CartaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CartasCorreio_DestinatarioId",
                table: "CartasCorreio",
                column: "DestinatarioId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CartasCorreio_DestinatarioId_Lida",
                table: "CartasCorreio",
                columns: new[] { "DestinatarioId", "Lida" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CartasCorreio_ExpiraEm",
                table: "CartasCorreio",
                column: "ExpiraEm"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AnexosItensCorreio");

            migrationBuilder.DropTable(name: "CartasCorreio");
        }
    }
}
