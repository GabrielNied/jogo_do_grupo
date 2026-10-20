using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Features.Personagem;
using JogoDoGrupo.Server.Features.Pets;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Npcs;
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

    private sealed class CarteiraCoroasMock(long saldoInicial) : ICarteiraCoroas
    {
        private long _saldo = saldoInicial;

        public Task<long> SaldoAsync(long personagemId, CancellationToken c) =>
            Task.FromResult(Interlocked.Read(ref _saldo));

        public Task<bool> DebitarAsync(long personagemId, long valor, CancellationToken c)
        {
            while (true)
            {
                var atual = Interlocked.Read(ref _saldo);
                if (atual < valor)
                {
                    return Task.FromResult(false);
                }

                if (Interlocked.CompareExchange(ref _saldo, atual - valor, atual) == atual)
                {
                    return Task.FromResult(true);
                }
            }
        }

        public Task CreditarAsync(long personagemId, long valor, CancellationToken c)
        {
            Interlocked.Add(ref _saldo, valor);
            return Task.CompletedTask;
        }
    }

    private sealed class PosicoesDoMundoMock : IPosicoesDoMundo
    {
        private readonly Dictionary<long, PosicaoNoMundo> _posicoes = new();

        public void Definir(long personagemId, PosicaoNoMundo pos) => _posicoes[personagemId] = pos;

        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao) =>
            _posicoes.TryGetValue(personagemId, out posicao);

        public IReadOnlyList<long> PersonagensNaArea(string area) => Array.Empty<long>();
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
        services.AddScoped<JogoDoGrupo.Server.Contratos.Itens.ICustodiaDeItens, CustodiaDeItens>();
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

    [Fact]
    public async Task ComprarPetCuidador_SemSaldo_Recusa()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 10);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 300;
            long contaId = 400;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(
                    CatalogoNpcs.AreaCidade,
                    CatalogoNpcs.CuidadorDePets.X,
                    CatalogoNpcs.CuidadorDePets.Y,
                    CatalogoNpcs.CuidadorDePets.Z,
                    0f
                )
            );

            var servico = new ServicoDePets(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, codigoErro, _, saldoRestante) = await servico.ComprarPetCuidadorAsync(
                personagemId,
                (int)EspecieDePet.GalinhaComum
            );

            Assert.False(sucesso);
            Assert.Equal(CodigosErroPets.SaldoInsuficiente, codigoErro);
            Assert.Equal(10, await carteira.SaldoAsync(personagemId, default));
            Assert.False(servico.PossuiPet(contaId, EspecieDePet.GalinhaComum));
        }
    }

    [Fact]
    public async Task ComprarPetCuidador_ComSaldo_DebitaEDesbloqueia()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 200);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 301;
            long contaId = 401;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(
                    CatalogoNpcs.AreaCidade,
                    CatalogoNpcs.CuidadorDePets.X,
                    CatalogoNpcs.CuidadorDePets.Y,
                    CatalogoNpcs.CuidadorDePets.Z,
                    0f
                )
            );

            var servico = new ServicoDePets(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, _, _, saldoRestante) = await servico.ComprarPetCuidadorAsync(
                personagemId,
                (int)EspecieDePet.CachorroComum
            );

            Assert.True(sucesso);
            Assert.Equal(100, saldoRestante); // 200 - 100
            Assert.Equal(100, await carteira.SaldoAsync(personagemId, default));
            Assert.True(servico.PossuiPet(contaId, EspecieDePet.CachorroComum));
            Assert.Contains((int)EspecieDePet.CachorroComum, servico.ObterColecao(contaId));
        }
    }

    [Fact]
    public async Task ComprarPetCuidador_LongeDoNpc_Recusa()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 500);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 302;
            long contaId = 402;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(CatalogoNpcs.AreaCidade, 100f, 0f, 100f, 0f) // longe de -14, 14
            );

            var servico = new ServicoDePets(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, codigoErro, _, _) = await servico.ComprarPetCuidadorAsync(
                personagemId,
                (int)EspecieDePet.GatoComum
            );

            Assert.False(sucesso);
            Assert.Equal(CodigosErroPets.LongeDoNpc, codigoErro);
            Assert.False(servico.PossuiPet(contaId, EspecieDePet.GatoComum));
            Assert.Equal(500, await carteira.SaldoAsync(personagemId, default));
        }
    }

    [Fact]
    public async Task ComprarPetCuidador_JaPossui_Recusa_E_NaoCobraSegundaVez()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 200);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 303;
            long contaId = 403;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(
                    CatalogoNpcs.AreaCidade,
                    CatalogoNpcs.CuidadorDePets.X,
                    CatalogoNpcs.CuidadorDePets.Y,
                    CatalogoNpcs.CuidadorDePets.Z,
                    0f
                )
            );

            var servico = new ServicoDePets(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso1, _, _, _) = await servico.ComprarPetCuidadorAsync(
                personagemId,
                (int)EspecieDePet.GalinhaComum
            );
            Assert.True(sucesso1);
            Assert.Equal(150, await carteira.SaldoAsync(personagemId, default)); // 200 - 50

            var (sucesso2, codigoErro2, _, _) = await servico.ComprarPetCuidadorAsync(
                personagemId,
                (int)EspecieDePet.GalinhaComum
            );
            Assert.False(sucesso2);
            Assert.Equal(CodigosErroPets.JaPossuiPet, codigoErro2);
            Assert.Equal(150, await carteira.SaldoAsync(personagemId, default)); // Saldo preservado
        }
    }

    [Fact]
    public async Task ComprarPetCuidador_Concorrente_CobraEDesbloqueiaUmaVez()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 100);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 304;
            long contaId = 404;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(
                    CatalogoNpcs.AreaCidade,
                    CatalogoNpcs.CuidadorDePets.X,
                    CatalogoNpcs.CuidadorDePets.Y,
                    CatalogoNpcs.CuidadorDePets.Z,
                    0f
                )
            );

            var servico = new ServicoDePets(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var tarefas = Enumerable
                .Range(0, 10)
                .Select(_ =>
                    servico.ComprarPetCuidadorAsync(personagemId, (int)EspecieDePet.GalinhaComum)
                )
                .ToArray();

            var resultados = await Task.WhenAll(tarefas);

            var sucessos = resultados.Count(r => r.Sucesso);
            var falhas = resultados.Count(r => !r.Sucesso);

            Assert.Equal(1, sucessos);
            Assert.Equal(9, falhas);
            Assert.Equal(50, await carteira.SaldoAsync(personagemId, default)); // 100 - 50
            Assert.True(servico.PossuiPet(contaId, EspecieDePet.GalinhaComum));
        }
    }

    [Fact]
    public void Avancar_com_posicoes_nulo_nao_lanca_excecao()
    {
        var servico = new ServicoDePets();
        var excecao = Record.Exception(() => servico.Avancar(0.1f));
        Assert.Null(excecao);
    }
}
