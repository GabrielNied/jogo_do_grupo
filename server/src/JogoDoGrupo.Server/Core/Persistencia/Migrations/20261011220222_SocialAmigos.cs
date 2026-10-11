using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class SocialAmigos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialAmizades",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    AmigoContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    NomeReferencia = table.Column<string>(
                        type: "TEXT",
                        maxLength: 32,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAmizades", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SocialIgnorados",
                columns: table => new
                {
                    IgnoradoId = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    IgnoradoContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialIgnorados", x => x.IgnoradoId);
                }
            );

            migrationBuilder.CreateTable(
                name: "SocialPedidosAmizade",
                columns: table => new
                {
                    PedidoId = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    ParaContaId = table.Column<long>(type: "INTEGER", nullable: false),
                    NomeAlvo = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPedidosAmizade", x => x.PedidoId);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SocialAmizades_ContaId_AmigoContaId",
                table: "SocialAmizades",
                columns: new[] { "ContaId", "AmigoContaId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SocialIgnorados_ContaId_IgnoradoContaId",
                table: "SocialIgnorados",
                columns: new[] { "ContaId", "IgnoradoContaId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SocialPedidosAmizade_DeContaId_ParaContaId",
                table: "SocialPedidosAmizade",
                columns: new[] { "DeContaId", "ParaContaId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SocialAmizades");

            migrationBuilder.DropTable(name: "SocialIgnorados");

            migrationBuilder.DropTable(name: "SocialPedidosAmizade");
        }
    }
}
