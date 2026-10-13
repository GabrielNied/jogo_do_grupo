using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AtributosERecursosComNomesNovos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Só renomeia: os pontos distribuídos e os recursos atuais ficam como estavam.
            // Vitalidade vira Vigor e Destreza vira Agilidade (atributos); vigor vira stamina e
            // energia vira mana (recursos). O EF gera os renames pela posição das colunas do mesmo
            // tipo, por isso os pares abaixo foram escritos à mão.
            migrationBuilder.RenameColumn(
                name: "Vigor",
                table: "RecursosPersonagem",
                newName: "Stamina"
            );

            migrationBuilder.RenameColumn(
                name: "Energia",
                table: "RecursosPersonagem",
                newName: "Mana"
            );

            migrationBuilder.RenameColumn(
                name: "UltimoGastoVigorEm",
                table: "RecursosPersonagem",
                newName: "UltimoGastoStaminaEm"
            );

            migrationBuilder.RenameColumn(
                name: "UltimoGastoEnergiaEm",
                table: "RecursosPersonagem",
                newName: "UltimoGastoManaEm"
            );

            migrationBuilder.RenameColumn(
                name: "VigorContadoAte",
                table: "RecursosPersonagem",
                newName: "StaminaContadaAte"
            );

            migrationBuilder.RenameColumn(
                name: "EnergiaContadaAte",
                table: "RecursosPersonagem",
                newName: "ManaContadaAte"
            );

            migrationBuilder.RenameColumn(
                name: "Vitalidade",
                table: "Personagens",
                newName: "Vigor"
            );

            migrationBuilder.RenameColumn(
                name: "Destreza",
                table: "Personagens",
                newName: "Agilidade"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Agilidade",
                table: "Personagens",
                newName: "Destreza"
            );

            migrationBuilder.RenameColumn(
                name: "Vigor",
                table: "Personagens",
                newName: "Vitalidade"
            );

            migrationBuilder.RenameColumn(
                name: "ManaContadaAte",
                table: "RecursosPersonagem",
                newName: "EnergiaContadaAte"
            );

            migrationBuilder.RenameColumn(
                name: "StaminaContadaAte",
                table: "RecursosPersonagem",
                newName: "VigorContadoAte"
            );

            migrationBuilder.RenameColumn(
                name: "UltimoGastoManaEm",
                table: "RecursosPersonagem",
                newName: "UltimoGastoEnergiaEm"
            );

            migrationBuilder.RenameColumn(
                name: "UltimoGastoStaminaEm",
                table: "RecursosPersonagem",
                newName: "UltimoGastoVigorEm"
            );

            migrationBuilder.RenameColumn(
                name: "Mana",
                table: "RecursosPersonagem",
                newName: "Energia"
            );

            migrationBuilder.RenameColumn(
                name: "Stamina",
                table: "RecursosPersonagem",
                newName: "Vigor"
            );
        }
    }
}
