using JogoDoGrupo.Server.Features.Economia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

public sealed class ServicoDeBarracasTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private static readonly int[] Modelos = ConfigMercado
        .ItensConhecidos.Keys.Where(id => id != CatalogoItens.EspadaDeTeste)
        .ToArray();
    private static int _proximoModelo = 50;

    private static int NovoModelo() =>
        Modelos[Interlocked.Increment(ref _proximoModelo) % Modelos.Length];

    private async Task<long> JogadorAsync(long coroas = 0, int itemId = 0, int itens = 0)
    {
        var id = await _ambiente.NovoPersonagemAsync();
        if (coroas > 0)
            await _ambiente.CreditarAsync(id, coroas);
        if (itens > 0)
            await _ambiente.DarItensAsync(id, itemId, itens);
        return id;
    }

    private async Task<long> InstanciaAsync(long personagem, int itemId) =>
        (await _ambiente.BolsaAsync(personagem)).First(e => e.ItemId == itemId).InstanciaId;

    [Fact]
    public async Task MontarBarraca_DentroDaPraca_Sucesso_E_ForaDaPraca_Recusa()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
        var jogador = await JogadorAsync(coroas: 100);

        var erroFora = barracas.MontarBarraca(jogador, "Dono", "Barraca Fora", 80f, 80f);
        Assert.Equal(CodigosErroEconomia.ForaDaPraca, erroFora);

        var erroPraca = barracas.MontarBarraca(
            jogador,
            "Dono",
            "Minha Barraca",
            ConfigBarraca.PracaCidadeX,
            ConfigBarraca.PracaCidadeZ
        );
        Assert.Null(erroPraca);

        var dados = await barracas.ConsultarBarracaAsync(jogador, default);
        Assert.NotNull(dados);
        Assert.Equal("Minha Barraca", dados!.Titulo);
        Assert.False(dados.EhVendedorOffline);

        barracas.DesmontarBarraca(jogador);
        var dadosAposDesmontar = await barracas.ConsultarBarracaAsync(jogador, default);
        Assert.Null(dadosAposDesmontar);
    }

    [Fact]
    public async Task ContratarVendedorOffline_DebitaDiarias_E_AtivaVendedor()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
        var jogador = await JogadorAsync(coroas: 100);

        // 3 diárias custam 150 coroas, saldo é 100
        var erroSaldo = await barracas.ContratarVendedorOfflineAsync(
            jogador,
            "Dono",
            "Manequim",
            3,
            ConfigBarraca.PracaCidadeX,
            ConfigBarraca.PracaCidadeZ,
            default
        );
        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, erroSaldo);

        // 2 diárias custam 100 coroas, saldo é 100
        var erroOk = await barracas.ContratarVendedorOfflineAsync(
            jogador,
            "Dono",
            "Manequim",
            2,
            ConfigBarraca.PracaCidadeX,
            ConfigBarraca.PracaCidadeZ,
            default
        );
        Assert.Null(erroOk);

        var saldoRestante = await _ambiente.SaldoAsync(jogador);
        Assert.Equal(0, saldoRestante);

        var dados = await barracas.ConsultarBarracaAsync(jogador, default);
        Assert.NotNull(dados);
        Assert.True(dados!.EhVendedorOffline);

        // Encerrar o vendedor offline
        var erroEncerrar = await barracas.EncerrarVendedorOfflineAsync(jogador, default);
        Assert.Null(erroEncerrar);

        var dadosAposEncerrar = await barracas.ConsultarBarracaAsync(jogador, default);
        Assert.Null(dadosAposEncerrar);
    }

    [Fact]
    public async Task ComprarDaBarraca_AplicaTaxaReduzidaDe3PorCento()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(coroas: 0, itemId: item, itens: 1);
        var comprador = await JogadorAsync(coroas: 200);
        var instancia = await InstanciaAsync(vendedor, item);

        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var mercado = escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>();
            var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();

            // Anuncia a 100 coroas
            var resVenda = await mercado.OferecerVendaAsync(vendedor, instancia, 100, default);
            Assert.Null(resVenda.Erro);

            // Monta barraca na praça
            var resMontar = barracas.MontarBarraca(
                vendedor,
                "Vendedor",
                "Barraca",
                ConfigBarraca.PracaCidadeX,
                ConfigBarraca.PracaCidadeZ
            );
            Assert.Null(resMontar);

            var ordens = await mercado.ListarOrdensDaBarracaAsync(vendedor, default);
            Assert.Single(ordens);
            var ordemId = ordens[0].Id;

            // Compra diretamente da barraca
            var resCompra = await barracas.ComprarDaBarracaAsync(
                comprador,
                ordemId,
                vendedor,
                default
            );
            Assert.Null(resCompra.Erro);
            Assert.Single(resCompra.Negocios);

            // Negócio concluído: taxa 3 coroas (3%), líquido 97
            var negocio = resCompra.Negocios[0];
            Assert.Equal(3, negocio.Taxa);
            Assert.Equal(100, negocio.PrecoUnitario);

            barracas.DesmontarBarraca(vendedor);
        }

        // Vendedor recebeu 97 coroas
        var saldoVendedor = await _ambiente.SaldoAsync(vendedor);
        Assert.Equal(97, saldoVendedor);

        // Comprador pagou 100 (200 - 100 = 100)
        var saldoComprador = await _ambiente.SaldoAsync(comprador);
        Assert.Equal(100, saldoComprador);

        // Comprador possui o item na bolsa
        var bolsaComprador = await _ambiente.BolsaAsync(comprador);
        Assert.Contains(bolsaComprador, e => e.ItemId == item);
    }

    [Fact]
    public async Task Concorrencia_CompraSimultaneaNaBarracaENoLeilao_VendeApenasUmaVez()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(coroas: 0, itemId: item, itens: 1);
        var compradorLeilao = await JogadorAsync(coroas: 200);
        var compradorBarraca = await JogadorAsync(coroas: 200);
        var instancia = await InstanciaAsync(vendedor, item);

        long ordemId;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var mercado = escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>();
            var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();

            await mercado.OferecerVendaAsync(vendedor, instancia, 100, default);
            barracas.MontarBarraca(
                vendedor,
                "Vendedor",
                "Barraca",
                ConfigBarraca.PracaCidadeX,
                ConfigBarraca.PracaCidadeZ
            );
            var ordens = await mercado.ListarOrdensDaBarracaAsync(vendedor, default);
            ordemId = ordens[0].Id;
        }

        // Tenta comprar concorrentemente
        var tarefaLeilao = Task.Run(async () =>
        {
            await using var escopo = servidor.Services.CreateAsyncScope();
            var mercado = escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>();
            return await mercado.ComprarOrdemAsync(compradorLeilao, ordemId, default);
        });

        var tarefaBarraca = Task.Run(async () =>
        {
            await using var escopo = servidor.Services.CreateAsyncScope();
            var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
            return await barracas.ComprarDaBarracaAsync(
                compradorBarraca,
                ordemId,
                vendedor,
                default
            );
        });

        var resultados = await Task.WhenAll(tarefaLeilao, tarefaBarraca);
        var res1 = resultados[0];
        var res2 = resultados[1];

        // Exatamente um teve sucesso, o outro falhou com erro de ordem indisponível/não encontrada
        var sucessos = resultados.Count(r => r.Erro == null);
        var falhas = resultados.Count(r => r.Erro != null);
        Assert.Equal(1, sucessos);
        Assert.Equal(1, falhas);

        // Apenas 1 exemplar existia e apenas 1 comprador tem o item
        var bolsa1 = await _ambiente.BolsaAsync(compradorLeilao);
        var bolsa2 = await _ambiente.BolsaAsync(compradorBarraca);
        var totalItensComprados =
            bolsa1.Count(e => e.ItemId == item) + bolsa2.Count(e => e.ItemId == item);
        Assert.Equal(1, totalItensComprados);

        // Limpa
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
            barracas.DesmontarBarraca(vendedor);
        }
    }

    [Fact]
    public async Task Desconectar_DesmontaPresencial_MasMantemVendedorOffline()
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
        var jogadorPresencial = await JogadorAsync(coroas: 100);
        var jogadorOffline = await JogadorAsync(coroas: 200);

        barracas.MontarBarraca(
            jogadorPresencial,
            "Presencial",
            "Loja",
            ConfigBarraca.PracaCidadeX,
            ConfigBarraca.PracaCidadeZ
        );
        await barracas.ContratarVendedorOfflineAsync(
            jogadorOffline,
            "Offline",
            "Manequim",
            1,
            ConfigBarraca.PracaCidadeX,
            ConfigBarraca.PracaCidadeZ,
            default
        );

        var ativasAntes = await barracas.ListarBarracasAtivasAsync(default);
        Assert.Equal(2, ativasAntes.Count);

        // Jogadores desconectam
        barracas.AoDesconectar(jogadorPresencial);
        barracas.AoDesconectar(jogadorOffline);

        var ativasDepois = await barracas.ListarBarracasAtivasAsync(default);
        Assert.Single(ativasDepois);
        Assert.Equal(jogadorOffline, ativasDepois[0].PersonagemId);
        Assert.True(ativasDepois[0].EhVendedorOffline);

        // Limpa
        await barracas.EncerrarVendedorOfflineAsync(jogadorOffline, default);
    }
}
