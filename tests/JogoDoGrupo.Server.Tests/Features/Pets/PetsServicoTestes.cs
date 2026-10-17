using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Features.Personagem;
using JogoDoGrupo.Server.Features.Pets;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Pets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Pets;

public class PetsServicoTestes
{
    private sealed class CombateMock(bool emCombate) : IEstadoDeCombate
    {
        public bool EmCombate(long personagemId) => emCombate;
    }

    private static (IServiceScopeFactory ScopeFactory, SqliteConnection Conexao) CriarBancoTeste()
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();

        var services = new ServiceCollection();
        services.AddDbContext<JogoDbContext>(opcoes =>
            opcoes.UseSqlite(
                conexao,
                o => o.MigrationsAssembly(typeof(JogoDbContext).Assembly.FullName)
            )
        );
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<JogoDbContext>().Database.EnsureCreated();
        }
        return (sp.GetRequiredService<IServiceScopeFactory>(), conexao);
    }

    private static void CriarPersonagem(JogoDbContext db, long personagemId, long contaId)
    {
        db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .Add(
                new JogoDoGrupo.Server.Features.Personagem.Personagem
                {
                    Id = personagemId,
                    ContaId = contaId,
                    Nome = "Heroi" + personagemId,
                    NomeNormalizado = "heroi" + personagemId,
                    Raca = Raca.Valdren,
                    CriadoEm = DateTime.UtcNow,
                }
            );
    }

    [Fact]
    public async Task UsarItemPet_ForaDeCombate_ConsomeItem_E_DesbloqueiaNaConta()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var servico = new ServicoDePets(scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<ItemInventario>()
                    .Add(
                        new ItemInventario
                        {
                            Id = 555,
                            PersonagemId = personagemId,
                            ItemId = CatalogoItens.SinoDoCervoJovem,
                        }
                    );
                await db.SaveChangesAsync();
            }

            Assert.False(servico.PossuiPet(contaId, EspecieDePet.CervoJovem));

            var usou = await servico.UsarItemPetAsync(contaId, personagemId, 555);

            Assert.True(usou);
            Assert.True(servico.PossuiPet(contaId, EspecieDePet.CervoJovem));
            Assert.Contains((int)EspecieDePet.CervoJovem, servico.ObterColecao(contaId));

            // Item foi consumido do inventário
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                var itemNoBanco = await db.Set<ItemInventario>()
                    .SingleOrDefaultAsync(i => i.Id == 555);
                Assert.Null(itemNoBanco);

                var petSalvo = await db.Set<PetDaConta>()
                    .SingleOrDefaultAsync(p =>
                        p.ContaId == contaId && p.Especie == (int)EspecieDePet.CervoJovem
                    );
                Assert.NotNull(petSalvo);
            }
        }
    }

    [Fact]
    public async Task UsarItemPet_EmCombate_Recusa_E_NaoConsome()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var combate = new CombateMock(emCombate: true);
            var servico = new ServicoDePets(combate: combate, scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<ItemInventario>()
                    .Add(
                        new ItemInventario
                        {
                            Id = 777,
                            PersonagemId = personagemId,
                            ItemId = CatalogoItens.SinoDoCervoJovem,
                        }
                    );
                await db.SaveChangesAsync();
            }

            var usou = await servico.UsarItemPetAsync(contaId, personagemId, 777);

            Assert.False(usou);
            Assert.False(servico.PossuiPet(contaId, EspecieDePet.CervoJovem));

            // Item permanece na bolsa
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                var itemNoBanco = await db.Set<ItemInventario>()
                    .SingleOrDefaultAsync(i => i.Id == 777);
                Assert.NotNull(itemNoBanco);
            }
        }
    }

    [Fact]
    public async Task UsarItemPet_QuandoJaPossui_Recusa_E_PreservaItemParaNegociar()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var servico = new ServicoDePets(scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<PetDaConta>()
                    .Add(
                        new PetDaConta
                        {
                            ContaId = contaId,
                            Especie = (int)EspecieDePet.CervoJovem,
                            DesbloqueadoEm = DateTime.UtcNow,
                        }
                    );
                db.Set<ItemInventario>()
                    .Add(
                        new ItemInventario
                        {
                            Id = 888,
                            PersonagemId = personagemId,
                            ItemId = CatalogoItens.SinoDoCervoJovem,
                        }
                    );
                await db.SaveChangesAsync();
            }

            var usou = await servico.UsarItemPetAsync(contaId, personagemId, 888);

            Assert.False(usou);

            // Item negociável permanece na bolsa
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                var itemNoBanco = await db.Set<ItemInventario>()
                    .SingleOrDefaultAsync(i => i.Id == 888);
                Assert.NotNull(itemNoBanco);
            }
        }
    }

    [Fact]
    public async Task UsarItemPet_ItemInvalido_Ou_NaoExiste_RetornaFalso()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var servico = new ServicoDePets(scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<ItemInventario>()
                    .Add(
                        new ItemInventario
                        {
                            Id = 999,
                            PersonagemId = personagemId,
                            ItemId = CatalogoItens.EspadaDeTeste,
                        }
                    );
                await db.SaveChangesAsync();
            }

            // Não existe
            Assert.False(await servico.UsarItemPetAsync(contaId, personagemId, 12345));

            // Não é pet
            Assert.False(await servico.UsarItemPetAsync(contaId, personagemId, 999));
        }
    }
}
