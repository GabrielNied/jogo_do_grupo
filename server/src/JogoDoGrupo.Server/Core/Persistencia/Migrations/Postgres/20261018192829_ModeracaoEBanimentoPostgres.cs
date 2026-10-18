using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ModeracaoEBanimentoPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanidaAte",
                table: "Contas",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "EhGm",
                table: "Contas",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "MotivoBanimento",
                table: "Contas",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "AuditoriaGm",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    GmContaId = table.Column<long>(type: "bigint", nullable: false),
                    GmPersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Acao = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Alvo = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Detalhes = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    ExecutadaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaGm", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    AutorContaId = table.Column<long>(type: "bigint", nullable: false),
                    AutorPersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    AlvoContaId = table.Column<long>(type: "bigint", nullable: true),
                    AlvoPersonagemId = table.Column<long>(type: "bigint", nullable: true),
                    AlvoNome = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
                    Motivo = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Detalhes = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    MetadadosContexto = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: false
                    ),
                    CriadaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaGm_ExecutadaEm",
                table: "AuditoriaGm",
                column: "ExecutadaEm"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaGm_GmContaId",
                table: "AuditoriaGm",
                column: "GmContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AlvoPersonagemId",
                table: "Denuncias",
                column: "AlvoPersonagemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AutorContaId",
                table: "Denuncias",
                column: "AutorContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_CriadaEm",
                table: "Denuncias",
                column: "CriadaEm"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditoriaGm");

            migrationBuilder.DropTable(name: "Denuncias");

            migrationBuilder.DropColumn(name: "BanidaAte", table: "Contas");

            migrationBuilder.DropColumn(name: "EhGm", table: "Contas");

            migrationBuilder.DropColumn(name: "MotivoBanimento", table: "Contas");
        }
    }
}
