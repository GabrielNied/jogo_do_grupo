using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CorreioEntreJogadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartasCorreio",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DestinatarioId = table.Column<long>(type: "INTEGER", nullable: false),
                    RemetenteId = table.Column<long>(type: "INTEGER", nullable: true),
                    RemetenteNome = table.Column<string>(
                        type: "TEXT",
                        maxLength: 40,
                        nullable: false
                    ),
                    Assunto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    Coroas = table.Column<long>(type: "INTEGER", nullable: false),
                    Lida = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnexosColetados = table.Column<bool>(type: "INTEGER", nullable: false),
                    Devolvida = table.Column<bool>(type: "INTEGER", nullable: false),
                    DoSistema = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnviadaEm = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<long>(type: "INTEGER", nullable: false),
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
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CartaId = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipamentoJson = table.Column<string>(type: "TEXT", nullable: true),
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
