using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Economia;

public class ConfigEconomiaTestes
{
    [Theory]
    [InlineData(TierItem.D, 2)]
    [InlineData(TierItem.C, 5)]
    [InlineData(TierItem.B, 10)]
    [InlineData(TierItem.A, 25)]
    [InlineData(TierItem.S, 60)]
    [InlineData(TierItem.SMais, 100)]
    public void PrecoVendaPorTier_RetornaPrecoEsperado(TierItem tier, long precoEsperado)
    {
        var preco = ConfigEconomia.PrecoVendaPorTier(tier);
        Assert.Equal(precoEsperado, preco);
    }

    [Fact]
    public void CalcularCustoReparo_SemDano_RetornaZero()
    {
        var custo = ConfigEconomia.CalcularCustoReparo(100, 100);
        Assert.Equal(0, custo);
    }

    [Fact]
    public void CalcularCustoReparo_DanoPequeno_RetornaMinimoDeUmaCoroa()
    {
        var custo = ConfigEconomia.CalcularCustoReparo(100, 99);
        Assert.Equal(1, custo);
    }

    [Fact]
    public void CalcularCustoReparo_DanoCompleto_CalculaCustoProporcionalLeve()
    {
        // 100 de dano: (100 + 4) / 5 = 20 coroas
        var custo = ConfigEconomia.CalcularCustoReparo(100, 0);
        Assert.Equal(20, custo);
    }

    [Fact]
    public void CatalogoPadrao_ContemItensValidosComPrecosPositivos()
    {
        Assert.NotEmpty(ConfigEconomia.CatalogoPadrao);
        foreach (var item in ConfigEconomia.CatalogoPadrao)
        {
            Assert.True(item.ItemId > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.Nome));
            Assert.True(item.PrecoCoroas > 0);
        }
    }

    [Fact]
    public void ObterItemLoja_RetornaItemOuNulo()
    {
        var pocao = ConfigEconomia.ObterItemLoja(CatalogoItens.PocaoDeTeste);
        Assert.NotNull(pocao);
        Assert.Equal(15, pocao.PrecoCoroas);

        var inexistente = ConfigEconomia.ObterItemLoja(999999);
        Assert.Null(inexistente);
    }

    [Fact]
    public void Equipamento_DurabilidadePadraoECalculoQuebrado()
    {
        var eq = new Equipamento();
        Assert.Equal(100, eq.DurabilidadeMaxima);
        Assert.Equal(100, eq.DurabilidadeAtual);
        Assert.False(eq.Quebrado);

        eq.DurabilidadeAtual = 0;
        Assert.True(eq.Quebrado);

        var entrada = new EntradaInventario
        {
            Equipamento = eq,
            Posicao = PosicaoEquipamento.Arma1,
        };
        // Quebrado não está ativa
        Assert.False(RegrasEquipamentos.EstaAtiva(entrada, 1));

        eq.DurabilidadeAtual = 50;
        Assert.False(eq.Quebrado);
        Assert.True(RegrasEquipamentos.EstaAtiva(entrada, 1));
    }
}
