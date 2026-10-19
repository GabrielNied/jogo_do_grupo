using JogoDoGrupo.Shared.Features.Economia;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Economia;

public sealed class RegrasBarracaTestes
{
    [Theory]
    [InlineData(0f, -10f, true)] // Centro da praça
    [InlineData(10f, -10f, true)] // 10m do centro (raio é 20m)
    [InlineData(0f, 10f, true)] // 20m do centro (na borda)
    [InlineData(25f, -10f, false)] // 25m do centro (fora)
    [InlineData(0f, 20f, false)] // 30m do centro (fora)
    public void DentroDaPraca_ValidaCoordenadasCorretamente(float x, float z, bool esperado)
    {
        Assert.Equal(esperado, ConfigBarraca.DentroDaPraca(x, z));
    }

    [Theory]
    [InlineData(10, 1)] // 10 * 3% = 0.3 -> teto 1
    [InlineData(33, 1)] // 33 * 3% = 0.99 -> teto 1
    [InlineData(34, 2)] // 34 * 3% = 1.02 -> teto 2
    [InlineData(100, 3)] // 100 * 3% = 3 -> 3
    [InlineData(200, 6)] // 200 * 3% = 6 -> 6
    [InlineData(1000, 30)] // 1000 * 3% = 30 -> 30
    public void TaxaDaVendaPresencial_AplicaTresPorCentoComTeto(long preco, long taxaEsperada)
    {
        Assert.Equal(taxaEsperada, ConfigBarraca.TaxaDaVendaPresencial(preco));
    }

    [Fact]
    public void TaxaPresencial_EhMenorQueTaxaGeralDoLeilao()
    {
        long preco = 100;
        long taxaLeilao = ConfigMercado.TaxaDaVenda(preco); // 5% = 5
        long taxaBarraca = ConfigBarraca.TaxaDaVendaPresencial(preco); // 3% = 3

        Assert.Equal(5, taxaLeilao);
        Assert.Equal(3, taxaBarraca);
        Assert.True(taxaBarraca < taxaLeilao);
    }

    [Fact]
    public void PodeMontarBarraca_RecusaForaDaPraca()
    {
        Assert.True(RegrasBarraca.PodeMontarBarraca(0f, -10f, out var erroSucesso));
        Assert.Null(erroSucesso);

        Assert.False(RegrasBarraca.PodeMontarBarraca(100f, 100f, out var erroFora));
        Assert.Equal(CodigosErroEconomia.ForaDaPraca, erroFora);
    }

    [Fact]
    public void PodeContratarVendedorOffline_ValidaDiasESaldo()
    {
        // 0 dias inválido
        Assert.False(
            RegrasBarraca.PodeContratarVendedorOffline(0, 0f, -10f, 1000, out var erroZero, out _)
        );
        Assert.Equal(CodigosErroEconomia.DiariaInvalida, erroZero);

        // Saldo insuficiente para 2 diárias (2 * 50 = 100 coroas)
        Assert.False(
            RegrasBarraca.PodeContratarVendedorOffline(2, 0f, -10f, 90, out var erroSaldo, out _)
        );
        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, erroSaldo);

        // Sucesso: 2 diárias com 100 coroas de saldo
        Assert.True(
            RegrasBarraca.PodeContratarVendedorOffline(
                2,
                0f,
                -10f,
                100,
                out var erroOk,
                out var custo
            )
        );
        Assert.Null(erroOk);
        Assert.Equal(100, custo);
    }
}
