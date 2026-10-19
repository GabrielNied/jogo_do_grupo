using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Economia;

public sealed class ConfigMercadoTestes
{
    [Theory]
    [InlineData(2, 1)]
    [InlineData(10, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(100, 5)]
    [InlineData(200, 10)]
    [InlineData(1000000000, 50000000)]
    public void TaxaDaVenda_ArredondaParaCima(long preco, long taxaEsperada)
    {
        Assert.Equal(taxaEsperada, ConfigMercado.TaxaDaVenda(preco));
    }

    [Fact]
    public void LiquidoDaVenda_EhOPrecoMenosATaxa_ENuncaMenorQueUm()
    {
        Assert.Equal(95, ConfigMercado.LiquidoDaVenda(100));
        Assert.True(ConfigMercado.LiquidoDaVenda(ConfigMercado.PrecoMinimo) >= 1);
    }

    [Fact]
    public void PrecoValido_RespeitaOsLimites()
    {
        Assert.False(ConfigMercado.PrecoValido(ConfigMercado.PrecoMinimo - 1));
        Assert.True(ConfigMercado.PrecoValido(ConfigMercado.PrecoMinimo));
        Assert.True(ConfigMercado.PrecoValido(ConfigMercado.PrecoMaximo));
        Assert.False(ConfigMercado.PrecoValido(ConfigMercado.PrecoMaximo + 1));
    }

    [Fact]
    public void ExpiracaoEmSegundos_SaoSeteDias()
    {
        Assert.Equal(7 * 86400L, ConfigMercado.ExpiracaoSegundos);
    }

    [Fact]
    public void Leiloeiro_RaioEmMetros()
    {
        Assert.True(
            ConfigMercado.DentroDoRaioLeiloeiro(ConfigMercado.LeiloeiroX, ConfigMercado.LeiloeiroZ)
        );
        Assert.False(
            ConfigMercado.DentroDoRaioLeiloeiro(
                ConfigMercado.LeiloeiroX + ConfigMercado.RaioParaInteragirMetros + 0.1f,
                ConfigMercado.LeiloeiroZ
            )
        );
    }

    [Fact]
    public void ItensConhecidos_TemOsDoCatalogoComNome()
    {
        Assert.Equal("Poção de teste", ConfigMercado.NomeDoItem(CatalogoItens.PocaoDeTeste));
        Assert.Null(ConfigMercado.NomeDoItem(-5));
    }
}
