using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JogoDoGrupo.Server.Core.Persistencia.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InicialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Usuario = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    UsuarioNormalizado = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    CriadaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "MontariasDaConta",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    TipoMontaria = table.Column<int>(type: "integer", nullable: false),
                    DesbloqueadaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MontariasDaConta", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "NomesReservados",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(
                        type: "character varying(16)",
                        maxLength: 16,
                        nullable: false
                    ),
                    NomeNormalizado = table.Column<string>(
                        type: "character varying(16)",
                        maxLength: 16,
                        nullable: false
                    ),
                    ExcluidoEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ReservadoAte = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NomesReservados", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Personagens",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(
                        type: "character varying(16)",
                        maxLength: 16,
                        nullable: false
                    ),
                    NomeNormalizado = table.Column<string>(
                        type: "character varying(16)",
                        maxLength: 16,
                        nullable: false
                    ),
                    Raca = table.Column<int>(type: "integer", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    TomDePele = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    Cabelo = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CorDeCabelo = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    XpAtual = table.Column<long>(type: "bigint", nullable: false),
                    PontosLivres = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personagens", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SkillsDoExemplar",
                columns: table => new
                {
                    InstanciaId = table.Column<long>(type: "bigint", nullable: false),
                    ModeloId = table.Column<int>(type: "integer", nullable: false),
                    SkillIds = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillsDoExemplar", x => x.InstanciaId);
                }
            );

            migrationBuilder.CreateTable(
                name: "SocialAmizades",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    AmigoContaId = table.Column<long>(type: "bigint", nullable: false),
                    NomeReferencia = table.Column<string>(
                        type: "character varying(32)",
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
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ContaId = table.Column<long>(type: "bigint", nullable: false),
                    IgnoradoContaId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
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
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    DeContaId = table.Column<long>(type: "bigint", nullable: false),
                    ParaContaId = table.Column<long>(type: "bigint", nullable: false),
                    NomeAlvo = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPedidosAmizade", x => x.PedidoId);
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentosArvores",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    PontosGastos = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentosArvores", x => x.PersonagemId);
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentosNos",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    NoId = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentosNos", x => new { x.PersonagemId, x.NoId });
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentosPerfis",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    RanksJson = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentosPerfis", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "EstadoEquipamentos",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    ConjuntoAtivo = table.Column<int>(type: "integer", nullable: false),
                    AbasBolsa = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 1
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoEquipamentos", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_EstadoEquipamentos_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ItensInventario",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Posicao = table.Column<int>(type: "integer", nullable: false),
                    Conteiner = table.Column<int>(type: "integer", nullable: false),
                    Aba = table.Column<int>(type: "integer", nullable: true),
                    Casa = table.Column<int>(type: "integer", nullable: true),
                    EquipamentoJson = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensInventario_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RecursosPersonagem",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Vida = table.Column<int>(type: "integer", nullable: false),
                    Stamina = table.Column<int>(type: "integer", nullable: false),
                    Mana = table.Column<int>(type: "integer", nullable: false),
                    UltimoGastoStaminaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UltimoGastoManaEm = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    StaminaContadaAte = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ManaContadaAte = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecursosPersonagem", x => x.PersonagemId);
                    table.ForeignKey(
                        name: "FK_RecursosPersonagem_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SaldosCoroas",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    Saldo = table.Column<long>(type: "bigint", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "SaldosMateriaisFusao",
                columns: table => new
                {
                    PersonagemId = table.Column<long>(type: "bigint", nullable: false),
                    EssenciasDungeon = table.Column<int>(type: "integer", nullable: false),
                    EssenciasRaid = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosMateriaisFusao", x => x.PersonagemId);
                    table.CheckConstraint(
                        "CK_SaldosMateriaisFusao_Dungeon",
                        "\"EssenciasDungeon\" >= 0"
                    );
                    table.CheckConstraint("CK_SaldosMateriaisFusao_Raid", "\"EssenciasRaid\" >= 0");
                    table.ForeignKey(
                        name: "FK_SaldosMateriaisFusao_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Contas_UsuarioNormalizado",
                table: "Contas",
                column: "UsuarioNormalizado",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_PersonagemId",
                table: "ItensInventario",
                column: "PersonagemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_PersonagemId_Conteiner_Aba_Casa",
                table: "ItensInventario",
                columns: new[] { "PersonagemId", "Conteiner", "Aba", "Casa" },
                unique: true,
                filter: "\"Casa\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ItensInventario_PersonagemId_Posicao",
                table: "ItensInventario",
                columns: new[] { "PersonagemId", "Posicao" },
                unique: true,
                filter: "\"Posicao\" <> 0"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MontariasDaConta_ContaId_TipoMontaria",
                table: "MontariasDaConta",
                columns: new[] { "ContaId", "TipoMontaria" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_NomesReservados_ContaId",
                table: "NomesReservados",
                column: "ContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NomesReservados_NomeNormalizado",
                table: "NomesReservados",
                column: "NomeNormalizado"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_ContaId",
                table: "Personagens",
                column: "ContaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_NomeNormalizado",
                table: "Personagens",
                column: "NomeNormalizado",
                unique: true
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

            migrationBuilder.CreateIndex(
                name: "IX_TalentosPerfis_PersonagemId",
                table: "TalentosPerfis",
                column: "PersonagemId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Contas");

            migrationBuilder.DropTable(name: "EstadoEquipamentos");

            migrationBuilder.DropTable(name: "ItensInventario");

            migrationBuilder.DropTable(name: "MontariasDaConta");

            migrationBuilder.DropTable(name: "NomesReservados");

            migrationBuilder.DropTable(name: "RecursosPersonagem");

            migrationBuilder.DropTable(name: "SaldosCoroas");

            migrationBuilder.DropTable(name: "SaldosMateriaisFusao");

            migrationBuilder.DropTable(name: "SkillsDoExemplar");

            migrationBuilder.DropTable(name: "SocialAmizades");

            migrationBuilder.DropTable(name: "SocialIgnorados");

            migrationBuilder.DropTable(name: "SocialPedidosAmizade");

            migrationBuilder.DropTable(name: "TalentosArvores");

            migrationBuilder.DropTable(name: "TalentosNos");

            migrationBuilder.DropTable(name: "TalentosPerfis");

            migrationBuilder.DropTable(name: "Personagens");
        }
    }
}
