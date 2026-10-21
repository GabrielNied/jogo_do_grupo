using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Features.Economia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

/// <summary>
/// O mercado (#176) com banco de verdade: casamento pelo melhor preço, taxa uma vez só,
/// custódia de item e coroas, cancelamento e concorrência sem duplicar item nem moeda.
/// O livro é global, então cada teste usa um modelo de item só dele.
/// </summary>
public sealed class MercadoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    // Um modelo por teste (sem repetir), para os livros não se misturarem.
    private static readonly int[] Modelos = ConfigMercado
        .ItensConhecidos.Keys.Where(id => id != CatalogoItens.EspadaDeTeste)
        .ToArray();
    private static int _proximoModelo = -1;

    private static int NovoModelo() => Modelos[Interlocked.Increment(ref _proximoModelo)];

    private async Task<T> MercadoAsync<T>(Func<ServicoDoMercado, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>());
    }

    private Task<ResultadoDoMercado> VenderAsync(long vendedor, long instancia, long preco) =>
        MercadoAsync(s => s.OferecerVendaAsync(vendedor, instancia, preco, default));

    private Task<ResultadoDoMercado> PedirAsync(long comprador, int item, int qtd, long preco) =>
        MercadoAsync(s => s.OferecerCompraAsync(comprador, item, qtd, preco, default));

    private Task<EstadoDoMercado> VerAsync(long personagem, int item = 0) =>
        MercadoAsync(s => s.ConsultarAsync(personagem, item, "", default));

    private async Task<long> InstanciaAsync(long personagem, int itemId) =>
        (await _ambiente.BolsaAsync(personagem)).First(e => e.ItemId == itemId).InstanciaId;

    /// <summary>Cria um personagem com coroas e, se pedido, itens do modelo na bolsa.</summary>
    private async Task<long> JogadorAsync(long coroas = 0, int itemId = 0, int itens = 0)
    {
        var id = await _ambiente.NovoPersonagemAsync();
        if (coroas > 0)
            await _ambiente.CreditarAsync(id, coroas);
        if (itens > 0)
            await _ambiente.DarItensAsync(id, itemId, itens);
        return id;
    }

    private async Task<int> NaBolsaAsync(long personagem, int itemId) =>
        (await _ambiente.BolsaAsync(personagem)).Count(e => e.ItemId == itemId);

    // ── Anunciar ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Vender_GuardaOItemNaOrdemENaoCobraTaxaAoAnunciar()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(coroas: 100, item, 1);
        var instancia = await InstanciaAsync(vendedor, item);

        var resultado = await VenderAsync(vendedor, instancia, 50);

        Assert.Null(resultado.Erro);
        Assert.Equal(100, await _ambiente.SaldoAsync(vendedor)); // anunciar é grátis
        Assert.Equal(0, await NaBolsaAsync(vendedor, item));
        var estado = await VerAsync(vendedor, item);
        var ordem = Assert.Single(estado.Vendas);
        Assert.Equal(50, ordem.PrecoUnitario);
        Assert.True(ordem.Minha);
        Assert.Equal(ordem.Id, Assert.Single(estado.Minhas).Id);
    }

    [Fact]
    public async Task Vender_ItemQueNaoEhDoJogador_Recusa()
    {
        var item = NovoModelo();
        var dono = await JogadorAsync(0, item, 1);
        var outro = await JogadorAsync();
        var instancia = await InstanciaAsync(dono, item);

        var resultado = await VenderAsync(outro, instancia, 10);

        Assert.Equal(CodigosErroEconomia.ItemNaoEncontrado, resultado.Erro);
        Assert.Equal(1, await NaBolsaAsync(dono, item));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ConfigMercado.PrecoMaximo + 1)]
    public async Task Vender_PrecoForaDosLimites_Recusa(long preco)
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        var instancia = await InstanciaAsync(vendedor, item);

        var resultado = await VenderAsync(vendedor, instancia, preco);

        Assert.Equal(CodigosErroEconomia.PrecoInvalido, resultado.Erro);
        Assert.Equal(1, await NaBolsaAsync(vendedor, item));
    }

    [Fact]
    public async Task Vender_AcimaDoLimiteDeOrdensAbertas_Recusa()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, ConfigMercado.OrdensAbertasMaximas + 1);
        var bolsa = await _ambiente.BolsaAsync(vendedor);
        for (var i = 0; i < ConfigMercado.OrdensAbertasMaximas; i++)
            Assert.Null((await VenderAsync(vendedor, bolsa[i].InstanciaId, 5)).Erro);

        var resultado = await VenderAsync(vendedor, bolsa[^1].InstanciaId, 5);

        Assert.Equal(CodigosErroEconomia.LimiteDeOrdens, resultado.Erro);
        Assert.Equal(1, await NaBolsaAsync(vendedor, item));
    }

    // ── Compra direta e a taxa ────────────────────────────────────────────────────

    [Fact]
    public async Task ComprarOrdem_PagaOPrecoEODonoRecebeMenosATaxaUmaVezSo()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        var comprador = await JogadorAsync(coroas: 1000);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 200);
        var ordem = (await VerAsync(comprador, item)).Vendas.Single();

        var resultado = await MercadoAsync(s => s.ComprarOrdemAsync(comprador, ordem.Id, default));

        Assert.Null(resultado.Erro);
        var negocio = Assert.Single(resultado.Negocios);
        Assert.Equal(10, negocio.Taxa); // 5% de 200
        Assert.Equal(800, await _ambiente.SaldoAsync(comprador));
        Assert.Equal(190, await _ambiente.SaldoAsync(vendedor));
        Assert.Equal(1, await NaBolsaAsync(comprador, item));
        Assert.Empty((await VerAsync(comprador, item)).Vendas);
        // O ouro que sumiu do jogo é só a taxa: 1000 - 800 - 190 = 10.
    }

    [Fact]
    public async Task ComprarOrdem_ExemplarAtravessaInteiroComOsMesmosAtributos()
    {
        var vendedor = await JogadorAsync(0, CatalogoItens.EspadaDeTeste, 1);
        var comprador = await JogadorAsync(coroas: 500);
        var antes = (await _ambiente.BolsaAsync(vendedor)).Single();
        await VenderAsync(vendedor, antes.InstanciaId, 100);
        var ordem = (await VerAsync(comprador, CatalogoItens.EspadaDeTeste)).Vendas.Single();
        Assert.NotNull(ordem.Item); // o anúncio mostra o exemplar

        await MercadoAsync(s => s.ComprarOrdemAsync(comprador, ordem.Id, default));

        var depois = (await _ambiente.BolsaAsync(comprador)).Single();
        Assert.Equal(antes.Equipamento!.Nome, depois.Equipamento!.Nome);
        Assert.Equal(antes.Equipamento.TierOrigem, depois.Equipamento.TierOrigem);
        Assert.Equal(antes.Equipamento.Atributos.Length, depois.Equipamento.Atributos.Length);
        Assert.Equal(0, await NaBolsaAsync(vendedor, CatalogoItens.EspadaDeTeste));
    }

    [Fact]
    public async Task ComprarOrdem_SemSaldo_NaoMoveNada()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        var comprador = await JogadorAsync(coroas: 50);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        var ordem = (await VerAsync(comprador, item)).Vendas.Single();

        var resultado = await MercadoAsync(s => s.ComprarOrdemAsync(comprador, ordem.Id, default));

        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, resultado.Erro);
        Assert.Equal(50, await _ambiente.SaldoAsync(comprador));
        Assert.Equal(0, await _ambiente.SaldoAsync(vendedor));
        Assert.Single((await VerAsync(comprador, item)).Vendas);
    }

    [Fact]
    public async Task ComprarOrdem_BolsaCheiaDoComprador_DesfazOPagamentoEMantemAOrdem()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        var comprador = await JogadorAsync(coroas: 500, item, 0);
        await _ambiente.DarItensAsync(comprador, CatalogoItens.PocaoDeTeste, 40);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        var ordem = (await VerAsync(comprador, item)).Vendas.Single();

        var resultado = await MercadoAsync(s => s.ComprarOrdemAsync(comprador, ordem.Id, default));

        Assert.Equal(CodigosErroEconomia.InventarioCheio, resultado.Erro);
        Assert.Equal(500, await _ambiente.SaldoAsync(comprador));
        Assert.Equal(0, await _ambiente.SaldoAsync(vendedor));
        Assert.Single((await VerAsync(comprador, item)).Vendas);
    }

    [Fact]
    public async Task ComprarOrdem_Propria_Recusa()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(coroas: 500, item, 1);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        var ordem = (await VerAsync(vendedor, item)).Vendas.Single();

        var resultado = await MercadoAsync(s => s.ComprarOrdemAsync(vendedor, ordem.Id, default));

        Assert.Equal(CodigosErroEconomia.OrdemPropria, resultado.Erro);
    }

    // ── Casamento pelo melhor preço ───────────────────────────────────────────────

    [Fact]
    public async Task Compra_CasaComAVendaMaisBarataEReembolsaADiferenca()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 3);
        var bolsa = await _ambiente.BolsaAsync(vendedor);
        await VenderAsync(vendedor, bolsa[0].InstanciaId, 30);
        await VenderAsync(vendedor, bolsa[1].InstanciaId, 10);
        await VenderAsync(vendedor, bolsa[2].InstanciaId, 20);
        var comprador = await JogadorAsync(coroas: 100);

        var resultado = await PedirAsync(comprador, item, 1, 50);

        Assert.Null(resultado.Erro);
        var negocio = Assert.Single(resultado.Negocios);
        Assert.Equal(10, negocio.PrecoUnitario); // a mais barata, pelo preço dela
        Assert.Equal(90, await _ambiente.SaldoAsync(comprador)); // pagou 10, voltou o resto
        Assert.Equal(1, await NaBolsaAsync(comprador, item));
        Assert.Equal(
            new long[] { 20, 30 },
            (await VerAsync(comprador, item)).Vendas.Select(v => v.PrecoUnitario).ToArray()
        );
        Assert.Empty((await VerAsync(comprador, item)).Compras); // a ordem cumprida sai do livro
    }

    [Fact]
    public async Task Compra_PedindoMais_CasaEmOrdemDePrecoEFicaAbertaComOResto()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 2);
        var bolsa = await _ambiente.BolsaAsync(vendedor);
        await VenderAsync(vendedor, bolsa[0].InstanciaId, 15);
        await VenderAsync(vendedor, bolsa[1].InstanciaId, 18);
        var comprador = await JogadorAsync(coroas: 100);

        var resultado = await PedirAsync(comprador, item, 3, 20);

        Assert.Equal(2, resultado.Negocios.Count);
        Assert.Equal(
            new long[] { 15, 18 },
            resultado.Negocios.Select(n => n.PrecoUnitario).ToArray()
        );
        // 3 x 20 guardados; pagou 15 + 18; voltaram 5 + 2; ficam 20 guardados na ordem aberta.
        Assert.Equal(100 - 60 + 7, await _ambiente.SaldoAsync(comprador));
        var restante = Assert.Single((await VerAsync(comprador, item)).Minhas);
        Assert.Equal(1, restante.Quantidade);
        Assert.False(restante.Venda);
        Assert.Equal(2, await NaBolsaAsync(comprador, item));
    }

    [Fact]
    public async Task Venda_CasaComACompraQuePagaMaisEPeloPrecoDela()
    {
        var item = NovoModelo();
        var c1 = await JogadorAsync(coroas: 100);
        var c2 = await JogadorAsync(coroas: 100);
        var c3 = await JogadorAsync(coroas: 100);
        await PedirAsync(c1, item, 1, 10);
        await PedirAsync(c2, item, 1, 40);
        await PedirAsync(c3, item, 1, 25);
        var vendedor = await JogadorAsync(0, item, 1);

        var resultado = await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 5);

        var negocio = Assert.Single(resultado.Negocios);
        Assert.Equal(c2, negocio.CompradorId);
        Assert.Equal(40, negocio.PrecoUnitario);
        Assert.Equal(2, negocio.Taxa);
        Assert.Equal(38, await _ambiente.SaldoAsync(vendedor));
        Assert.Equal(60, await _ambiente.SaldoAsync(c2));
        Assert.Equal(1, await NaBolsaAsync(c2, item));
        Assert.Equal(2, (await VerAsync(vendedor, item)).Compras.Length);
        Assert.Empty((await VerAsync(vendedor, item)).Vendas);
    }

    [Fact]
    public async Task Casamento_ComPrecoIgual_FicaComAOrdemMaisAntiga()
    {
        var item = NovoModelo();
        var primeiro = await JogadorAsync(coroas: 100);
        var segundo = await JogadorAsync(coroas: 100);
        await PedirAsync(primeiro, item, 1, 30);
        await PedirAsync(segundo, item, 1, 30);
        var vendedor = await JogadorAsync(0, item, 1);

        var resultado = await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 30);

        Assert.Equal(primeiro, Assert.Single(resultado.Negocios).CompradorId);
        Assert.Equal(1, await NaBolsaAsync(primeiro, item));
        Assert.Equal(0, await NaBolsaAsync(segundo, item));
    }

    [Fact]
    public async Task Casamento_NuncaEntreOrdensDoMesmoPersonagem()
    {
        var item = NovoModelo();
        var jogador = await JogadorAsync(coroas: 100, item, 1);
        await PedirAsync(jogador, item, 1, 50);

        var resultado = await VenderAsync(jogador, await InstanciaAsync(jogador, item), 10);

        Assert.Empty(resultado.Negocios);
        var estado = await VerAsync(jogador, item);
        Assert.Single(estado.Vendas);
        Assert.Single(estado.Compras);
    }

    [Fact]
    public async Task Casamento_PrecosQueNaoSeCruzam_NaoCasam()
    {
        var item = NovoModelo();
        var comprador = await JogadorAsync(coroas: 100);
        await PedirAsync(comprador, item, 1, 10);
        var vendedor = await JogadorAsync(0, item, 1);

        var resultado = await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 11);

        Assert.Empty(resultado.Negocios);
        Assert.Equal(90, await _ambiente.SaldoAsync(comprador)); // 10 guardados na ordem
    }

    [Fact]
    public async Task Venda_CasaComCompradorComBolsaCheia_PulaEFicaAbertaNoLivro()
    {
        var item = NovoModelo();
        var cheio = await JogadorAsync(coroas: 100);
        await PedirAsync(cheio, item, 1, 50);
        await _ambiente.DarItensAsync(cheio, CatalogoItens.PocaoDeTeste, 40);
        var vendedor = await JogadorAsync(0, item, 1);

        var resultado = await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 10);

        Assert.Empty(resultado.Negocios);
        Assert.Single((await VerAsync(vendedor, item)).Vendas);
        Assert.Equal(0, await NaBolsaAsync(cheio, item));
    }

    // ── Cancelar ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancelar_Venda_DevolveOItemSemTaxa()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(coroas: 10, item, 1);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        var ordem = (await VerAsync(vendedor, item)).Minhas.Single();

        var resultado = await MercadoAsync(s => s.CancelarAsync(vendedor, ordem.Id, default));

        Assert.Null(resultado.Erro);
        Assert.Equal(1, await NaBolsaAsync(vendedor, item));
        Assert.Equal(10, await _ambiente.SaldoAsync(vendedor));
        Assert.Empty((await VerAsync(vendedor, item)).Vendas);
    }

    [Fact]
    public async Task Cancelar_Compra_DevolveTodasAsCoroasGuardadas()
    {
        var item = NovoModelo();
        var comprador = await JogadorAsync(coroas: 100);
        await PedirAsync(comprador, item, 4, 20);
        Assert.Equal(20, await _ambiente.SaldoAsync(comprador));
        var ordem = (await VerAsync(comprador, item)).Minhas.Single();

        var resultado = await MercadoAsync(s => s.CancelarAsync(comprador, ordem.Id, default));

        Assert.Null(resultado.Erro);
        Assert.Equal(100, await _ambiente.SaldoAsync(comprador));
    }

    [Fact]
    public async Task Cancelar_OrdemDeOutroOuJaCancelada_Recusa()
    {
        var item = NovoModelo();
        var dono = await JogadorAsync(coroas: 100);
        var intruso = await JogadorAsync();
        await PedirAsync(dono, item, 1, 20);
        var ordem = (await VerAsync(dono, item)).Minhas.Single();

        var deOutro = await MercadoAsync(s => s.CancelarAsync(intruso, ordem.Id, default));
        await MercadoAsync(s => s.CancelarAsync(dono, ordem.Id, default));
        var denovo = await MercadoAsync(s => s.CancelarAsync(dono, ordem.Id, default));

        Assert.Equal(CodigosErroEconomia.OrdemNaoEncontrada, deOutro.Erro);
        Assert.Equal(CodigosErroEconomia.OrdemIndisponivel, denovo.Erro);
        Assert.Equal(100, await _ambiente.SaldoAsync(dono)); // devolvido uma vez só
    }

    [Fact]
    public async Task Cancelar_VendaComBolsaCheia_GuardaOItemAteHaverEspaco()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        await _ambiente.DarItensAsync(vendedor, CatalogoItens.PocaoDeTeste, 40); // enche a bolsa
        var ordem = (await VerAsync(vendedor, item)).Minhas.Single();

        await MercadoAsync(s => s.CancelarAsync(vendedor, ordem.Id, default));

        var pendente = Assert.Single((await VerAsync(vendedor, item)).Minhas);
        Assert.True(pendente.AguardaDevolucao);
        Assert.Equal(0, await NaBolsaAsync(vendedor, item));

        // Abre uma casa e pede a devolução.
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();
            var qualquer = (await custodia.ListarNegociaveisAsync(vendedor, default)).First();
            await custodia.RetirarAsync(vendedor, qualquer.InstanciaId, default);
        }

        var devolucao = await MercadoAsync(s => s.ReceberDevolucoesAsync(vendedor, default));

        Assert.Equal((vendedor, item), Assert.Single(devolucao.Devolvidos));
        Assert.Equal(1, await NaBolsaAsync(vendedor, item));
        Assert.Empty((await VerAsync(vendedor, item)).Minhas);
        // Pedir de novo não entrega duas vezes.
        var outra = await MercadoAsync(s => s.ReceberDevolucoesAsync(vendedor, default));
        Assert.Empty(outra.Devolvidos);
        Assert.Equal(1, await NaBolsaAsync(vendedor, item));
    }

    // ── Compra: validações ────────────────────────────────────────────────────────

    [Fact]
    public async Task Pedir_SemSaldo_NaoCriaOrdem()
    {
        var item = NovoModelo();
        var comprador = await JogadorAsync(coroas: 59);

        var resultado = await PedirAsync(comprador, item, 3, 20);

        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, resultado.Erro);
        Assert.Equal(59, await _ambiente.SaldoAsync(comprador));
        Assert.Empty((await VerAsync(comprador, item)).Compras);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(ConfigMercado.QuantidadeMaximaDaCompra + 1, 10)]
    public async Task Pedir_QuantidadeInvalida_Recusa(int quantidade, long preco)
    {
        var comprador = await JogadorAsync(coroas: 100000);

        var resultado = await PedirAsync(comprador, NovoModelo(), quantidade, preco);

        Assert.Equal(CodigosErroEconomia.QuantidadeInvalida, resultado.Erro);
    }

    [Fact]
    public async Task Pedir_ModeloDesconhecido_Recusa()
    {
        var comprador = await JogadorAsync(coroas: 100);

        var resultado = await PedirAsync(comprador, 987654, 1, 10);

        Assert.Equal(CodigosErroEconomia.ItemNaoNegociavel, resultado.Erro);
        Assert.Equal(100, await _ambiente.SaldoAsync(comprador));
    }

    // ── Concorrência ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Concorrencia_VariosCompradoresDeUmaMesmaVenda_SoUmLeva()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 100);
        var ordem = (await VerAsync(vendedor, item)).Vendas.Single();
        var compradores = new List<long>();
        for (var i = 0; i < 8; i++)
            compradores.Add(await JogadorAsync(coroas: 100));

        var resultados = await Task.WhenAll(
            compradores.Select(c =>
                Task.Run(() => MercadoAsync(s => s.ComprarOrdemAsync(c, ordem.Id, default)))
            )
        );

        Assert.Equal(1, resultados.Count(r => r.Erro == null));
        var comItem = 0;
        long saldos = 0;
        foreach (var c in compradores)
        {
            comItem += await NaBolsaAsync(c, item);
            saldos += await _ambiente.SaldoAsync(c);
        }

        Assert.Equal(1, comItem);
        Assert.Equal(0, await NaBolsaAsync(vendedor, item));
        // Moeda conservada: 800 no início = 700 que sobraram + 95 do vendedor + 5 de taxa.
        Assert.Equal(700, saldos);
        Assert.Equal(95, await _ambiente.SaldoAsync(vendedor));
    }

    [Fact]
    public async Task Concorrencia_VendasECompras_ConservamItensECoroas()
    {
        var item = NovoModelo();
        const int pares = 6;
        var vendedores = new List<long>();
        var compradores = new List<long>();
        for (var i = 0; i < pares; i++)
        {
            vendedores.Add(await JogadorAsync(0, item, 1));
            compradores.Add(await JogadorAsync(coroas: 200));
        }

        var tarefas = new List<Task<ResultadoDoMercado>>();
        for (var i = 0; i < pares; i++)
        {
            var vendedor = vendedores[i];
            var comprador = compradores[i];
            var instancia = await InstanciaAsync(vendedor, item);
            tarefas.Add(Task.Run(() => VenderAsync(vendedor, instancia, 50 + i)));
            tarefas.Add(Task.Run(() => PedirAsync(comprador, item, 1, 60)));
        }

        var resultados = await Task.WhenAll(tarefas);
        Assert.All(resultados, r => Assert.Null(r.Erro));

        // Itens: nas bolsas ou em ordens de venda abertas, nunca a mais nem a menos.
        var nasBolsas = 0;
        long saldos = 0;
        foreach (var id in vendedores.Concat(compradores))
        {
            nasBolsas += await NaBolsaAsync(id, item);
            saldos += await _ambiente.SaldoAsync(id);
        }

        var estado = await VerAsync(vendedores[0], item);
        var emVenda = estado.Vendas.Length;
        Assert.Equal(pares, nasBolsas + emVenda);

        // Coroas: as dos compradores (200 cada), mais as guardadas nas compras abertas, mais as
        // taxas destruídas, é o total do começo.
        var guardadas = estado.Compras.Sum(o => o.PrecoUnitario * o.Quantidade);
        var taxas = resultados.SelectMany(r => r.Negocios).Sum(n => n.Taxa);
        Assert.Equal(pares * 200L, saldos + guardadas + taxas);
        Assert.Equal(pares - emVenda, resultados.SelectMany(r => r.Negocios).Count());
    }

    // ── Livro e filtro ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consultar_FiltraPorNomeEPorModelo()
    {
        var item = NovoModelo();
        var vendedor = await JogadorAsync(0, item, 1);
        await VenderAsync(vendedor, await InstanciaAsync(vendedor, item), 10);
        var nome = ConfigMercado.NomeDoItem(item)!;

        var porNome = await MercadoAsync(s =>
            s.ConsultarAsync(vendedor, 0, nome.ToUpperInvariant(), default)
        );
        var semAchar = await MercadoAsync(s =>
            s.ConsultarAsync(vendedor, 0, "zzz-nada-assim", default)
        );

        Assert.Contains(porNome.Vendas, o => o.ItemId == item);
        Assert.DoesNotContain(semAchar.Vendas, o => o.ItemId == item);
        Assert.NotEmpty(porNome.Modelos);
        Assert.Equal(ConfigMercado.TaxaVendaPercentual, porNome.TaxaVendaPercentual);
    }
}
