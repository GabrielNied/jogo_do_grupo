using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class EmailERecuperacaoDeConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoRecuperacaoExpiraEm",
                table: "Contas",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacaoHash",
                table: "Contas",
                type: "TEXT",
                maxLength: 256,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "CodigoRecuperacaoTentativas",
                table: "Contas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Contas",
                type: "TEXT",
                maxLength: 254,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "EmailNormalizado",
                table: "Contas",
                type: "TEXT",
                maxLength: 254,
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Contas_EmailNormalizado",
                table: "Contas",
                column: "EmailNormalizado",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Contas_EmailNormalizado", table: "Contas");

            migrationBuilder.DropColumn(name: "CodigoRecuperacaoExpiraEm", table: "Contas");

            migrationBuilder.DropColumn(name: "CodigoRecuperacaoHash", table: "Contas");

            migrationBuilder.DropColumn(name: "CodigoRecuperacaoTentativas", table: "Contas");

            migrationBuilder.DropColumn(name: "Email", table: "Contas");

            migrationBuilder.DropColumn(name: "EmailNormalizado", table: "Contas");
        }
    }
}
