using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Economia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

public sealed class BancoGuildaTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private sealed class FakeConsultaGuilda : IConsultaDeGuilda
    {
        public readonly Dictionary<long, (long GuildaId, CargoDaGuilda Cargo)> Membros = [];
        public readonly Dictionary<long, string> NomesGuildas = [];

        public long ObterGuildaId(long personagemId) =>
            Membros.TryGetValue(personagemId, out var m) ? m.GuildaId : 0;

        public string ObterNomeGuilda(long guildaId) =>
            NomesGuildas.TryGetValue(guildaId, out var nome) ? nome : "Guilda dos Bravos";

        public CargoDaGuilda? ObterCargo(long personagemId) =>
            Membros.TryGetValue(personagemId, out var m) ? m.Cargo : null;
    }

    private ServicoDeBancoGuilda CriarServico(
        IServiceProvider sp,
        IConsultaDeGuilda consultaGuilda,
        Action<long>? aoMovimentarCoroas = null
    )
    {
        return new ServicoDeBancoGuilda(
            sp.GetRequiredService<JogoDbContext>(),
            consultaGuilda,
            sp.GetRequiredService<ICustodiaDeItens>(),
            sp.GetRequiredService<ICarteiraCoroas>(),
            sp.GetRequiredService<IDiretorioDePersonagens>(),
            obterPosicao: null, // Sem restrição de posição para testes unitários de regra
            aoMovimentarCoroas: aoMovimentarCoroas
        );
    }

    [Fact]
    public async Task SemGuilda_ConsultarEDepositar_RecusaComSemGuilda()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var fake = new FakeConsultaGuilda();
        var servico = CriarServico(escopo.ServiceProvider, fake);

        var personagem = await _ambiente.NovoPersonagemAsync();

        var resConsulta = await servico.ConsultarAsync(personagem, default);
        Assert.False(resConsulta.Sucesso);
        Assert.Equal(CodigosErroEconomia.SemGuilda, resConsulta.CodigoErro);

        var resDeposito = await servico.DepositarCoroasAsync(personagem, 50, default);
        Assert.False(resDeposito.Sucesso);
        Assert.Equal(CodigosErroEconomia.SemGuilda, resDeposito.CodigoErro);
    }

    [Fact]
    public async Task Membro_PodeDepositarItemECoroas_MasNaoPodeRetirar()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var fake = new FakeConsultaGuilda();
        var servico = CriarServico(escopo.ServiceProvider, fake);

        var membro = await _ambiente.NovoPersonagemAsync();
        const long guildaId = 100;
        fake.Membros[membro] = (guildaId, CargoDaGuilda.Membro);

        await _ambiente.CreditarAsync(membro, 200);
        await _ambiente.DarItensAsync(membro, CatalogoItens.EspadaDeTeste, 1);
        var bolsa = await _ambiente.BolsaAsync(membro);
        var instanciaId = bolsa.First(e => e.ItemId == CatalogoItens.EspadaDeTeste).InstanciaId;

        // 1. Depositar coroas
        var resCoroas = await servico.DepositarCoroasAsync(membro, 100, default);
        Assert.True(resCoroas.Sucesso);
        Assert.Equal(100, resCoroas.Estado!.SaldoCoroas);
        Assert.Equal(100, await _ambiente.SaldoAsync(membro));

        // 2. Depositar item
        var resItem = await servico.DepositarItemAsync(membro, instanciaId, 1, 0, default);
        Assert.True(resItem.Sucesso);
        Assert.Single(resItem.Estado!.Itens);
        Assert.Equal(CatalogoItens.EspadaDeTeste, resItem.Estado.Itens[0].ItemId);
        var itemBancoId = resItem.Estado.Itens[0].Id;

        // 3. Tentar retirar item como membro -> Recusado
        var resRetiraItem = await servico.RetirarItemAsync(membro, itemBancoId, default);
        Assert.False(resRetiraItem.Sucesso);
        Assert.Equal(CodigosErroEconomia.SemPermissaoBancoGuilda, resRetiraItem.CodigoErro);

        // 4. Tentar retirar coroas como membro -> Recusado
        var resRetiraCoroas = await servico.RetirarCoroasAsync(membro, 50, default);
        Assert.False(resRetiraCoroas.Sucesso);
        Assert.Equal(CodigosErroEconomia.SemPermissaoBancoGuilda, resRetiraCoroas.CodigoErro);
    }

    [Fact]
    public async Task LiderEOficial_PodemRetirarItensECoroas()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var fake = new FakeConsultaGuilda();
        var servico = CriarServico(escopo.ServiceProvider, fake);

        var lider = await _ambiente.NovoPersonagemAsync();
        var oficial = await _ambiente.NovoPersonagemAsync();
        const long guildaId = 101;
        fake.Membros[lider] = (guildaId, CargoDaGuilda.Lider);
        fake.Membros[oficial] = (guildaId, CargoDaGuilda.Oficial);

        // Líder abastece o banco
        await _ambiente.CreditarAsync(lider, 300);
        await _ambiente.DarItensAsync(lider, CatalogoItens.EscudoDeTeste, 1);
        var escudoId = (await _ambiente.BolsaAsync(lider))
            .First(e => e.ItemId == CatalogoItens.EscudoDeTeste)
            .InstanciaId;

        await servico.DepositarCoroasAsync(lider, 250, default);
        var depItem = await servico.DepositarItemAsync(lider, escudoId, 1, 0, default);
        var itemBancoId = depItem.Estado!.Itens[0].Id;

        // Oficial retira metade das coroas
        var resRetiraCoroas = await servico.RetirarCoroasAsync(oficial, 100, default);
        Assert.True(resRetiraCoroas.Sucesso);
        Assert.Equal(150, resRetiraCoroas.Estado!.SaldoCoroas);
        Assert.Equal(100, await _ambiente.SaldoAsync(oficial));

        // Líder retira o item
        var resRetiraItem = await servico.RetirarItemAsync(lider, itemBancoId, default);
        Assert.True(resRetiraItem.Sucesso);
        Assert.Empty(resRetiraItem.Estado!.Itens);

        var bolsaLider = await _ambiente.BolsaAsync(lider);
        Assert.Contains(bolsaLider, e => e.ItemId == CatalogoItens.EscudoDeTeste);
    }

    [Fact]
    public async Task RetirarItem_BolsaCheia_RollbackConservaItemNoBanco()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var fake = new FakeConsultaGuilda();
        var servico = CriarServico(escopo.ServiceProvider, fake);

        var oficial = await _ambiente.NovoPersonagemAsync();
        const long guildaId = 102;
        fake.Membros[oficial] = (guildaId, CargoDaGuilda.Oficial);

        // Deposita um item
        await _ambiente.DarItensAsync(oficial, CatalogoItens.PocaoDeTeste, 1);
        var pocaoId = (await _ambiente.BolsaAsync(oficial))
            .First(e => e.ItemId == CatalogoItens.PocaoDeTeste)
            .InstanciaId;
        var dep = await servico.DepositarItemAsync(oficial, pocaoId, 1, 0, default);
        var itemBancoId = dep.Estado!.Itens[0].Id;

        // Enche a bolsa do oficial com 40 itens
        await _ambiente.DarItensAsync(oficial, CatalogoItens.MoedaDeTeste, 40);

        // Tenta retirar com a bolsa cheia
        var ret = await servico.RetirarItemAsync(oficial, itemBancoId, default);
        Assert.False(ret.Sucesso);
        Assert.Equal(CodigosErroEconomia.InventarioCheio, ret.CodigoErro);

        // Confere que o item permaneceu no banco da guilda sem se perder
        var estado = await servico.ConsultarAsync(oficial, default);
        Assert.True(estado.Sucesso);
        Assert.Single(estado.Estado!.Itens);
        Assert.Equal(CatalogoItens.PocaoDeTeste, estado.Estado.Itens[0].ItemId);
    }

    [Fact]
    public async Task RetirarCoroas_SaldoInsuficienteNoBanco_Recusa()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var fake = new FakeConsultaGuilda();
        var servico = CriarServico(escopo.ServiceProvider, fake);

        var lider = await _ambiente.NovoPersonagemAsync();
        const long guildaId = 103;
        fake.Membros[lider] = (guildaId, CargoDaGuilda.Lider);

        await _ambiente.CreditarAsync(lider, 50);
        await servico.DepositarCoroasAsync(lider, 50, default);

        var ret = await servico.RetirarCoroasAsync(lider, 100, default);
        Assert.False(ret.Sucesso);
        Assert.Equal(CodigosErroEconomia.CoroasInsuficientesNoBanco, ret.CodigoErro);
    }
}
