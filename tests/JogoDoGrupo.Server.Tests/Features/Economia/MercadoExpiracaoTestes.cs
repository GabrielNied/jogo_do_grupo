using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Features.Economia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

/// <summary>
/// Expiração do mercado (#176). Classe à parte porque o teste adianta o relógio do servidor, o
/// que valeria para os outros testes que dividem o mesmo servidor.
/// </summary>
public sealed class MercadoExpiracaoTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> MercadoAsync<T>(Func<ServicoDoMercado, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>());
    }

    private void AvancarOTempo(TimeSpan tempo) =>
        servidor.Services.GetRequiredService<RelogioDoMercado>().Avancar(tempo);

    [Fact]
    public async Task Expirar_AosSeteDias_DevolveOItemEAsCoroasEOrdemSaiDoLivro()
    {
        var vendedor = await _ambiente.NovoPersonagemAsync();
        var comprador = await _ambiente.NovoPersonagemAsync();
        await _ambiente.DarItensAsync(vendedor, CatalogoItens.MoedaDeTeste);
        await _ambiente.CreditarAsync(comprador, 100);
        var instancia = (await _ambiente.BolsaAsync(vendedor)).Single().InstanciaId;
        await MercadoAsync(s => s.OferecerVendaAsync(vendedor, instancia, 500, default));
        // Uma compra de outro modelo, que também vai vencer (as coroas voltam).
        await MercadoAsync(s =>
            s.OferecerCompraAsync(comprador, CatalogoItens.PocaoDeTeste, 2, 30, default)
        );
        Assert.Equal(40, await _ambiente.SaldoAsync(comprador));

        // Seis dias: nada acontece.
        AvancarOTempo(TimeSpan.FromDays(ConfigMercado.ExpiracaoDias - 1));
        var seisDias = await MercadoAsync(s => s.ExpirarVencidasAsync(default));
        Assert.DoesNotContain(
            seisDias.Encerradas,
            e => e.PersonagemId == vendedor || e.PersonagemId == comprador
        );

        AvancarOTempo(TimeSpan.FromDays(2));
        var resultado = await MercadoAsync(s => s.ExpirarVencidasAsync(default));

        Assert.Equal(
            2,
            resultado.Encerradas.Count(e =>
                e.Expirou && (e.PersonagemId == vendedor || e.PersonagemId == comprador)
            )
        );
        Assert.Equal(
            1,
            (await _ambiente.BolsaAsync(vendedor)).Count(e =>
                e.ItemId == CatalogoItens.MoedaDeTeste
            )
        );
        Assert.Equal(100, await _ambiente.SaldoAsync(comprador));
        var estado = await MercadoAsync(s => s.ConsultarAsync(vendedor, 0, "", default));
        Assert.DoesNotContain(estado.Vendas, o => o.Minha);
        Assert.Empty(estado.Minhas);

        // Rodar a varredura de novo não devolve nada em dobro.
        var denovo = await MercadoAsync(s => s.ExpirarVencidasAsync(default));
        Assert.DoesNotContain(
            denovo.Encerradas,
            e => e.PersonagemId == vendedor || e.PersonagemId == comprador
        );
        Assert.Equal(100, await _ambiente.SaldoAsync(comprador));
    }

    [Fact]
    public async Task OrdemVencida_NaoCasaMesmoAntesDaVarredura()
    {
        var vendedor = await _ambiente.NovoPersonagemAsync();
        var comprador = await _ambiente.NovoPersonagemAsync();
        await _ambiente.DarItensAsync(vendedor, CatalogoItens.SinoDoLoboFilhote);
        await _ambiente.CreditarAsync(comprador, 100);
        var instancia = (await _ambiente.BolsaAsync(vendedor)).Single().InstanciaId;
        await MercadoAsync(s => s.OferecerVendaAsync(vendedor, instancia, 10, default));
        var ordem = (
            await MercadoAsync(s => s.ConsultarAsync(comprador, 0, "Lobo Filhote", default))
        ).Vendas.Single();
        AvancarOTempo(TimeSpan.FromDays(ConfigMercado.ExpiracaoDias + 1));

        var compra = await MercadoAsync(s => s.ComprarOrdemAsync(comprador, ordem.Id, default));
        var pedido = await MercadoAsync(s =>
            s.OferecerCompraAsync(comprador, CatalogoItens.SinoDoLoboFilhote, 1, 50, default)
        );

        Assert.Equal(CodigosErroEconomia.OrdemIndisponivel, compra.Erro);
        Assert.Empty(pedido.Negocios);
        Assert.Equal(50, await _ambiente.SaldoAsync(comprador)); // só os 50 da compra aberta
    }

    [Fact]
    public async Task Expirar_VendaComBolsaCheia_GuardaOItemEEntregaNoLogin()
    {
        var vendedor = await _ambiente.NovoPersonagemAsync();
        await _ambiente.DarItensAsync(vendedor, CatalogoItens.SinoDoTatuPequeno);
        var instancia = (await _ambiente.BolsaAsync(vendedor)).Single().InstanciaId;
        await MercadoAsync(s => s.OferecerVendaAsync(vendedor, instancia, 10, default));
        await _ambiente.DarItensAsync(vendedor, CatalogoItens.PocaoDeTeste, 40); // bolsa cheia
        AvancarOTempo(TimeSpan.FromDays(ConfigMercado.ExpiracaoDias + 1));

        var resultado = await MercadoAsync(s => s.ExpirarVencidasAsync(default));

        Assert.DoesNotContain(resultado.Devolvidos, d => d.PersonagemId == vendedor);
        var estado = await MercadoAsync(s => s.ConsultarAsync(vendedor, 0, "", default));
        Assert.True(Assert.Single(estado.Minhas).AguardaDevolucao);

        // Abre espaço; o servidor tenta de novo quando o jogador entra (o mesmo método do login).
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();
            var qualquer = (await custodia.ListarNegociaveisAsync(vendedor, default)).First(e =>
                e.ItemId == CatalogoItens.PocaoDeTeste
            );
            await custodia.RetirarAsync(vendedor, qualquer.InstanciaId, default);
        }

        var devolucao = await MercadoAsync(s => s.ReceberDevolucoesAsync(vendedor, default));

        Assert.Equal(vendedor, Assert.Single(devolucao.Devolvidos).PersonagemId);
        Assert.Equal(
            1,
            (await _ambiente.BolsaAsync(vendedor)).Count(e =>
                e.ItemId == CatalogoItens.SinoDoTatuPequeno
            )
        );
    }
}
