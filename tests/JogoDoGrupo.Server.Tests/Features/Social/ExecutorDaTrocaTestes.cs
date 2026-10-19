using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Troca direta com banco de verdade (#175): atomicidade e concorrência. Item e moeda nunca
/// duplicam nem se perdem, aconteça o que acontecer no meio.
/// </summary>
public sealed class ExecutorDaTrocaTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private IExecutorDaTroca Executor => servidor.Services.GetRequiredService<IExecutorDaTroca>();

    private async Task<(long A, long B)> DoisAsync()
    {
        var a = await _ambiente.NovoPersonagemAsync();
        var b = await _ambiente.NovoPersonagemAsync();
        return (a, b);
    }

    private static int Contar(List<EntradaInventario> bolsa, int itemId) =>
        bolsa.Count(e => e.ItemId == itemId);

    [Fact]
    public async Task Troca_MovimentaCoroasEItensDosDoisLadosDeUmaVez()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 50);
        await _ambiente.CreditarAsync(b, 10);
        await _ambiente.DarItensAsync(a, CatalogoItens.PocaoDeTeste);
        await _ambiente.DarItensAsync(b, CatalogoItens.EspadaDeTeste);
        var pocao = (await _ambiente.BolsaAsync(a)).Single().InstanciaId;
        var espada = (await _ambiente.BolsaAsync(b)).Single();

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(30, [pocao]),
            b,
            new OfertaPedida(0, [espada.InstanciaId]),
            default
        );

        Assert.Null(resultado.Erro);
        Assert.Equal(20, await _ambiente.SaldoAsync(a));
        Assert.Equal(40, await _ambiente.SaldoAsync(b));
        var bolsaA = await _ambiente.BolsaAsync(a);
        var bolsaB = await _ambiente.BolsaAsync(b);
        Assert.Equal(CatalogoItens.EspadaDeTeste, bolsaA.Single().ItemId);
        Assert.Equal(CatalogoItens.PocaoDeTeste, bolsaB.Single().ItemId);
        // O exemplar atravessou inteiro: os atributos sorteados da espada são os mesmos.
        Assert.Equal(espada.Equipamento.Nome, bolsaA.Single().Equipamento.Nome);
        Assert.Equal(espada.Equipamento.TierOrigem, bolsaA.Single().Equipamento.TierOrigem);
    }

    [Fact]
    public async Task Troca_DeCoroasPorCoroas_NaoDependeDaOrdem()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 100);
        await _ambiente.CreditarAsync(b, 100);

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(100, []),
            b,
            new OfertaPedida(100, []),
            default
        );

        Assert.Null(resultado.Erro);
        Assert.Equal(100, await _ambiente.SaldoAsync(a));
        Assert.Equal(100, await _ambiente.SaldoAsync(b));
    }

    [Fact]
    public async Task Atomicidade_ItemDoSegundoLadoSumiu_DesfazTudoDoPrimeiro()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 50);
        await _ambiente.CreditarAsync(b, 5);
        await _ambiente.DarItensAsync(a, CatalogoItens.PocaoDeTeste);
        var pocao = (await _ambiente.BolsaAsync(a)).Single().InstanciaId;

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(30, [pocao]),
            b,
            new OfertaPedida(5, [999_999_999]),
            default
        );

        Assert.Equal(CodigosErroTroca.ItemIndisponivel, resultado.Erro);
        Assert.Equal(b, resultado.PersonagemDoErro);
        Assert.Equal(50, await _ambiente.SaldoAsync(a));
        Assert.Equal(5, await _ambiente.SaldoAsync(b));
        Assert.Equal(pocao, (await _ambiente.BolsaAsync(a)).Single().InstanciaId);
        Assert.Empty(await _ambiente.BolsaAsync(b));
    }

    [Fact]
    public async Task Atomicidade_SaldoInsuficiente_NaoMoveNada()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 10);
        await _ambiente.DarItensAsync(b, CatalogoItens.PocaoDeTeste);
        var pocao = (await _ambiente.BolsaAsync(b)).Single().InstanciaId;

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(11, []),
            b,
            new OfertaPedida(0, [pocao]),
            default
        );

        Assert.Equal(CodigosErroTroca.SaldoInsuficiente, resultado.Erro);
        Assert.Equal(a, resultado.PersonagemDoErro);
        Assert.Equal(10, await _ambiente.SaldoAsync(a));
        Assert.Equal(0, await _ambiente.SaldoAsync(b));
        Assert.Equal(pocao, (await _ambiente.BolsaAsync(b)).Single().InstanciaId);
        Assert.Empty(await _ambiente.BolsaAsync(a));
    }

    [Fact]
    public async Task Atomicidade_BolsaCheiaDeQuemRecebe_DesfazCoroasEItens()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 50);
        await _ambiente.DarItensAsync(a, CatalogoItens.EspadaDeTeste);
        var espada = (await _ambiente.BolsaAsync(a)).Single().InstanciaId;
        // Enche a bolsa do B (uma aba de 40 casas) com poções.
        await _ambiente.DarItensAsync(b, CatalogoItens.PocaoDeTeste, 40);

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(20, [espada]),
            b,
            new OfertaPedida(0, []),
            default
        );

        Assert.Equal(CodigosErroTroca.BolsaCheia, resultado.Erro);
        Assert.Equal(b, resultado.PersonagemDoErro);
        Assert.Equal(50, await _ambiente.SaldoAsync(a));
        Assert.Equal(0, await _ambiente.SaldoAsync(b));
        Assert.Equal(espada, (await _ambiente.BolsaAsync(a)).Single().InstanciaId);
        Assert.Equal(40, (await _ambiente.BolsaAsync(b)).Count);
    }

    [Fact]
    public async Task BolsaCheia_MasComTrocaDeUmPorUm_FuncionaPorqueLiberaACasa()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.DarItensAsync(a, CatalogoItens.EspadaDeTeste);
        await _ambiente.DarItensAsync(b, CatalogoItens.PocaoDeTeste, 40);
        var espada = (await _ambiente.BolsaAsync(a)).Single().InstanciaId;
        var pocao = (await _ambiente.BolsaAsync(b)).First().InstanciaId;

        var resultado = await Executor.ExecutarAsync(
            a,
            new OfertaPedida(0, [espada]),
            b,
            new OfertaPedida(0, [pocao]),
            default
        );

        Assert.Null(resultado.Erro);
        Assert.Equal(40, (await _ambiente.BolsaAsync(b)).Count);
        Assert.Equal(1, Contar(await _ambiente.BolsaAsync(b), CatalogoItens.EspadaDeTeste));
        Assert.Single(await _ambiente.BolsaAsync(a));
    }

    [Fact]
    public async Task Validar_ItemQueNaoEhDoJogador_EhIndisponivel()
    {
        var (a, b) = await DoisAsync();
        await _ambiente.DarItensAsync(b, CatalogoItens.PocaoDeTeste);
        var daB = (await _ambiente.BolsaAsync(b)).Single().InstanciaId;

        var (erro, _) = await Executor.ValidarOfertaAsync(a, new OfertaPedida(0, [daB]), default);

        Assert.Equal(CodigosErroTroca.ItemIndisponivel, erro);
    }

    [Fact]
    public async Task Validar_CoroasAcimaDoSaldo_RecusaESemErroAceita()
    {
        var (a, _) = await DoisAsync();
        await _ambiente.CreditarAsync(a, 10);

        var (erroAlto, _) = await Executor.ValidarOfertaAsync(a, new OfertaPedida(11, []), default);
        var (erroOk, oferta) = await Executor.ValidarOfertaAsync(
            a,
            new OfertaPedida(10, []),
            default
        );

        Assert.Equal(CodigosErroTroca.SaldoInsuficiente, erroAlto);
        Assert.Null(erroOk);
        Assert.Equal(10, oferta!.Coroas);
    }

    [Fact]
    public async Task Concorrencia_MesmoItemEmDuasTrocas_UmaSoVenceENadaDuplica()
    {
        var (a, b) = await DoisAsync();
        var c = await _ambiente.NovoPersonagemAsync();
        await _ambiente.DarItensAsync(a, CatalogoItens.EspadaDeTeste);
        var espada = (await _ambiente.BolsaAsync(a)).Single().InstanciaId;

        var tarefas = new[] { b, c, b, c, b, c }
            .Select(dono =>
                Task.Run(() =>
                    Executor.ExecutarAsync(
                        a,
                        new OfertaPedida(0, [espada]),
                        dono,
                        OfertaPedida.Vazia,
                        default
                    )
                )
            )
            .ToArray();
        var resultados = await Task.WhenAll(tarefas);

        Assert.Equal(1, resultados.Count(r => r.Erro == null));
        Assert.All(
            resultados.Where(r => r.Erro != null),
            r => Assert.Equal(CodigosErroTroca.ItemIndisponivel, r.Erro)
        );
        var total =
            (await _ambiente.BolsaAsync(a)).Count
            + (await _ambiente.BolsaAsync(b)).Count
            + (await _ambiente.BolsaAsync(c)).Count;
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task Concorrencia_MesmasCoroasEmDuasTrocas_NaoGastaDuasVezes()
    {
        var (a, b) = await DoisAsync();
        var c = await _ambiente.NovoPersonagemAsync();
        await _ambiente.CreditarAsync(a, 100);

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 6)
                .Select(i =>
                    Task.Run(() =>
                        Executor.ExecutarAsync(
                            a,
                            new OfertaPedida(100, []),
                            i % 2 == 0 ? b : c,
                            OfertaPedida.Vazia,
                            default
                        )
                    )
                )
        );

        Assert.Equal(1, resultados.Count(r => r.Erro == null));
        Assert.Equal(0, await _ambiente.SaldoAsync(a));
        Assert.Equal(100, await _ambiente.SaldoAsync(b) + await _ambiente.SaldoAsync(c));
    }
}
