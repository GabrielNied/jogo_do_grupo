using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Montarias;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Montarias;

public class MontariasServicoTestes
{
    private sealed class CarteiraCoroasMock : ICarteiraCoroas
    {
        private long _saldo;

        public CarteiraCoroasMock(long saldoInicial)
        {
            _saldo = saldoInicial;
        }

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

    private sealed class ControleVelocidadeSpy : IControleDeVelocidadeDoMundo
    {
        public long UltimoPersonagemId { get; private set; }
        public float UltimoAndar { get; private set; }
        public float UltimoCorrer { get; private set; }
        public int Chamadas { get; private set; }

        public bool DefinirVelocidades(long personagemId, float andar, float correr)
        {
            UltimoPersonagemId = personagemId;
            UltimoAndar = andar;
            UltimoCorrer = correr;
            Chamadas++;
            return true;
        }
    }

    private sealed class CombateMock(bool emCombate) : IEstadoDeCombate
    {
        public bool EmCombate(long personagemId) => emCombate;
    }

    [Fact]
    public void ChamarMontaria_AumentaVelocidade_E_MarcaComoMontado()
    {
        var velocidade = new ControleVelocidadeSpy();
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade);
        long personagemId = 42;

        var sucesso = servico.ChamarMontaria(personagemId);

        Assert.True(sucesso);
        Assert.True(servico.EstaMontado(personagemId));
        Assert.Equal(RegrasMontaria.TipoPadrao, servico.TipoDaMontaria(personagemId));
        Assert.Equal(1, velocidade.Chamadas);
        Assert.Equal(personagemId, velocidade.UltimoPersonagemId);
        Assert.Equal(RegrasMontaria.VelocidadeMontariaPadrao, velocidade.UltimoCorrer);
    }

    [Fact]
    public void Desmontar_RestauraVelocidadePadrao_E_RemoveEstado()
    {
        var velocidade = new ControleVelocidadeSpy();
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade);
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        var desmontou = servico.Desmontar(personagemId);

        Assert.True(desmontou);
        Assert.False(servico.EstaMontado(personagemId));
        Assert.Null(servico.TipoDaMontaria(personagemId));
        Assert.Equal(2, velocidade.Chamadas);
        Assert.Equal(RegrasVelocidade.CorrerPadrao, velocidade.UltimoCorrer);
    }

    [Fact]
    public void ChamarMontaria_Recusa_SeEstiverEmCombate()
    {
        var velocidade = new ControleVelocidadeSpy();
        var combate = new CombateMock(emCombate: true);
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade, combate: combate);
        long personagemId = 42;

        var sucesso = servico.ChamarMontaria(personagemId);

        Assert.False(sucesso);
        Assert.False(servico.EstaMontado(personagemId));
        Assert.Equal(0, velocidade.Chamadas);
    }

    [Fact]
    public void Desconectar_LimpaEstadoMontado()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        Assert.True(servico.EstaMontado(personagemId));

        servico.Desconectar(personagemId);
        Assert.False(servico.EstaMontado(personagemId));
    }

    [Fact]
    public void EquiparMontaria_AlteraMontariaEquipada_E_ChamaComNovaMontaria()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        Assert.Equal(RegrasMontaria.TipoPadrao, servico.ObterMontariaEquipada(personagemId));

        var equipou = servico.EquiparMontaria(personagemId, RegrasMontaria.TipoNoturno);
        Assert.True(equipou);
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.ObterMontariaEquipada(personagemId));

        servico.ChamarMontaria(personagemId);
        Assert.True(servico.EstaMontado(personagemId));
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.TipoDaMontaria(personagemId));
    }

    [Fact]
    public void EquiparMontaria_RecusaTipoInvalido()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        var equipou = servico.EquiparMontaria(personagemId, 999);
        Assert.False(equipou);
        Assert.Equal(RegrasMontaria.TipoPadrao, servico.ObterMontariaEquipada(personagemId));
    }

    [Fact]
    public void EquiparMontaria_QuandoJaMontado_AtualizaTipoAtivoImediatamente()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        Assert.Equal(RegrasMontaria.TipoCastanho, servico.TipoDaMontaria(personagemId));

        servico.EquiparMontaria(personagemId, RegrasMontaria.TipoNoturno);
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.TipoDaMontaria(personagemId));
    }

    private static (
        IServiceScopeFactory ScopeFactory,
        Microsoft.Data.Sqlite.SqliteConnection Conexao
    ) CriarBancoTeste()
    {
        var conexao = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>(o =>
            Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(
                o,
                conexao
            )
        );
        services.AddScoped<
            JogoDoGrupo.Server.Contratos.Itens.ICustodiaDeItens,
            JogoDoGrupo.Server.Features.Itens.CustodiaDeItens
        >();
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            scope
                .ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>()
                .Database.EnsureCreated();
        }
        return (
            sp.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
            conexao
        );
    }

    private static void CriarPersonagem(
        JogoDoGrupo.Server.Core.Persistencia.JogoDbContext db,
        long personagemId,
        long contaId
    )
    {
        db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .Add(
                new JogoDoGrupo.Server.Features.Personagem.Personagem
                {
                    Id = personagemId,
                    ContaId = contaId,
                    Nome = "Heroi" + personagemId,
                    NomeNormalizado = "heroi" + personagemId,
                    Raca = JogoDoGrupo.Shared.Features.Personagem.Raca.Valdren,
                    CriadoEm = DateTime.UtcNow,
                }
            );
    }

    [Fact]
    public async Task UsarItemMontaria_ForaDeCombate_ConsomeItem_E_DesbloqueiaNaConta()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var servico = new ServicoDeMontarias(scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>()
                    .Add(
                        new JogoDoGrupo.Server.Features.Itens.ItemInventario
                        {
                            Id = 555,
                            PersonagemId = personagemId,
                            ItemId = JogoDoGrupo
                                .Shared
                                .Features
                                .Itens
                                .CatalogoItens
                                .RedeasDoCorcelNoturno,
                        }
                    );
                await db.SaveChangesAsync();
            }

            Assert.False(servico.PossuiMontaria(contaId, RegrasMontaria.TipoNoturno));

            var usou = await servico.UsarItemMontariaAsync(contaId, personagemId, 555);

            Assert.True(usou);
            Assert.True(servico.PossuiMontaria(contaId, RegrasMontaria.TipoNoturno));
            Assert.Contains(RegrasMontaria.TipoNoturno, servico.ObterColecao(contaId));

            // Item foi consumido do banco
            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                var itemNoBanco =
                    await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(
                        db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>(),
                        i => i.Id == 555
                    );
                Assert.Null(itemNoBanco);
            }
        }
    }

    [Fact]
    public async Task UsarItemMontaria_EmCombate_Recusa_E_NaoConsome()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var combate = new CombateMock(emCombate: true);
            var servico = new ServicoDeMontarias(combate: combate, scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>()
                    .Add(
                        new JogoDoGrupo.Server.Features.Itens.ItemInventario
                        {
                            Id = 777,
                            PersonagemId = personagemId,
                            ItemId = JogoDoGrupo
                                .Shared
                                .Features
                                .Itens
                                .CatalogoItens
                                .RedeasDoCorcelNoturno,
                        }
                    );
                await db.SaveChangesAsync();
            }

            var usou = await servico.UsarItemMontariaAsync(contaId, personagemId, 777);

            Assert.False(usou);
            Assert.False(servico.PossuiMontaria(contaId, RegrasMontaria.TipoNoturno));

            // Item permanece intacto
            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                var itemNoBanco =
                    await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(
                        db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>(),
                        i => i.Id == 777
                    );
                Assert.NotNull(itemNoBanco);
            }
        }
    }

    [Fact]
    public async Task UsarItemMontaria_QuandoJaPossui_Recusa_E_PreservaItemParaNegociar()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var servico = new ServicoDeMontarias(scopeFactory: scopeFactory);
            long contaId = 100;
            long personagemId = 200;

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                db.Set<MontariaDaConta>()
                    .Add(
                        new MontariaDaConta
                        {
                            ContaId = contaId,
                            TipoMontaria = RegrasMontaria.TipoNoturno,
                            DesbloqueadaEm = DateTime.UtcNow,
                        }
                    );
                db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>()
                    .Add(
                        new JogoDoGrupo.Server.Features.Itens.ItemInventario
                        {
                            Id = 888,
                            PersonagemId = personagemId,
                            ItemId = JogoDoGrupo
                                .Shared
                                .Features
                                .Itens
                                .CatalogoItens
                                .RedeasDoCorcelNoturno,
                        }
                    );
                await db.SaveChangesAsync();
            }

            var usou = await servico.UsarItemMontariaAsync(contaId, personagemId, 888);

            Assert.False(usou);

            // Item negociável permanece na bolsa
            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                var itemNoBanco =
                    await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(
                        db.Set<JogoDoGrupo.Server.Features.Itens.ItemInventario>(),
                        i => i.Id == 888
                    );
                Assert.NotNull(itemNoBanco);
            }
        }
    }

    [Fact]
    public async Task ComprarMontariaEstabulo_SemCoroas_Recusa_E_NaoDesbloqueia()
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
                    CatalogoNpcs.Cavalarico.X,
                    CatalogoNpcs.Cavalarico.Y,
                    CatalogoNpcs.Cavalarico.Z,
                    0f
                )
            );

            var servico = new ServicoDeMontarias(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, codigoErro, _, _) = await servico.ComprarMontariaEstabuloAsync(
                personagemId,
                RegrasMontaria.TipoMulaDeCarga
            );

            Assert.False(sucesso);
            Assert.Equal(CodigosErroMontarias.SaldoInsuficiente, codigoErro);
            Assert.False(servico.PossuiMontaria(contaId, RegrasMontaria.TipoMulaDeCarga));
            Assert.Equal(10, await carteira.SaldoAsync(personagemId, default));
        }
    }

    [Fact]
    public async Task ComprarMontariaEstabulo_ComSaldoEPerto_DebitaCoroas_E_Desbloqueia()
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
                    CatalogoNpcs.Cavalarico.X,
                    CatalogoNpcs.Cavalarico.Y,
                    CatalogoNpcs.Cavalarico.Z,
                    0f
                )
            );

            var servico = new ServicoDeMontarias(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, _, _, saldoRestante) = await servico.ComprarMontariaEstabuloAsync(
                personagemId,
                RegrasMontaria.TipoCavaloComum
            );

            Assert.True(sucesso);
            Assert.Equal(50, saldoRestante); // 200 - 150
            Assert.Equal(50, await carteira.SaldoAsync(personagemId, default));
            Assert.True(servico.PossuiMontaria(contaId, RegrasMontaria.TipoCavaloComum));
            Assert.Contains(RegrasMontaria.TipoCavaloComum, servico.ObterColecao(contaId));
        }
    }

    [Fact]
    public async Task ComprarMontariaEstabulo_LongeDoNpc_Recusa()
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
                new PosicaoNoMundo(CatalogoNpcs.AreaCidade, 100f, 0f, 100f, 0f) // longe de -14, 8
            );

            var servico = new ServicoDeMontarias(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso, codigoErro, _, _) = await servico.ComprarMontariaEstabuloAsync(
                personagemId,
                RegrasMontaria.TipoMulaDeCarga
            );

            Assert.False(sucesso);
            Assert.Equal(CodigosErroMontarias.LongeDoNpc, codigoErro);
            Assert.False(servico.PossuiMontaria(contaId, RegrasMontaria.TipoMulaDeCarga));
            Assert.Equal(500, await carteira.SaldoAsync(personagemId, default));
        }
    }

    [Fact]
    public async Task ComprarMontariaEstabulo_JaPossui_Recusa_E_NaoCobraSegundaVez()
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
                    CatalogoNpcs.Cavalarico.X,
                    CatalogoNpcs.Cavalarico.Y,
                    CatalogoNpcs.Cavalarico.Z,
                    0f
                )
            );

            var servico = new ServicoDeMontarias(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var (sucesso1, _, _, _) = await servico.ComprarMontariaEstabuloAsync(
                personagemId,
                RegrasMontaria.TipoMulaDeCarga
            );
            Assert.True(sucesso1);
            Assert.Equal(120, await carteira.SaldoAsync(personagemId, default)); // 200 - 80

            var (sucesso2, codigoErro2, _, _) = await servico.ComprarMontariaEstabuloAsync(
                personagemId,
                RegrasMontaria.TipoMulaDeCarga
            );
            Assert.False(sucesso2);
            Assert.Equal(CodigosErroMontarias.JaPossuiMontaria, codigoErro2);
            Assert.Equal(120, await carteira.SaldoAsync(personagemId, default)); // Saldo preservado
        }
    }

    [Fact]
    public async Task ComprarMontariaEstabulo_Concorrente_CobraEDesbloqueiaUmaVez()
    {
        var (scopeFactory, conexao) = CriarBancoTeste();
        using (conexao)
        {
            var carteira = new CarteiraCoroasMock(saldoInicial: 150);
            var posicoes = new PosicoesDoMundoMock();
            long personagemId = 304;
            long contaId = 404;
            posicoes.Definir(
                personagemId,
                new PosicaoNoMundo(
                    CatalogoNpcs.AreaCidade,
                    CatalogoNpcs.Cavalarico.X,
                    CatalogoNpcs.Cavalarico.Y,
                    CatalogoNpcs.Cavalarico.Z,
                    0f
                )
            );

            var servico = new ServicoDeMontarias(
                posicoes: posicoes,
                carteiraCoroas: carteira,
                scopeFactory: scopeFactory
            );

            using (var scope = scopeFactory.CreateScope())
            {
                var db =
                    scope.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Core.Persistencia.JogoDbContext>();
                CriarPersonagem(db, personagemId, contaId);
                await db.SaveChangesAsync();
            }

            var tarefas = Enumerable
                .Range(0, 10)
                .Select(_ =>
                    servico.ComprarMontariaEstabuloAsync(
                        personagemId,
                        RegrasMontaria.TipoCavaloComum
                    )
                )
                .ToArray();

            var resultados = await Task.WhenAll(tarefas);

            var sucessos = resultados.Count(r => r.Sucesso);
            var falhas = resultados.Count(r => !r.Sucesso);

            Assert.Equal(1, sucessos);
            Assert.Equal(9, falhas);
            Assert.Equal(0, await carteira.SaldoAsync(personagemId, default)); // 150 - 150
            Assert.True(servico.PossuiMontaria(contaId, RegrasMontaria.TipoCavaloComum));
        }
    }
}
