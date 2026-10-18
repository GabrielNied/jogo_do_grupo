using JogoDoGrupo.Shared.Features.Itens;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasBancoTestes
{
    [Fact]
    public void CapacidadeBancoInicial_tem_1_aba_e_40_casas()
    {
        var capacidade = RegrasBanco.CapacidadeBancoInicial();
        Assert.Equal(1, capacidade.Abas);
        Assert.Equal(40, capacidade.CasasPorAba);
        Assert.Equal(40, capacidade.Total);
        Assert.True(capacidade.Contem(0, 0));
        Assert.True(capacidade.Contem(0, 39));
        Assert.False(capacidade.Contem(0, 40));
        Assert.False(capacidade.Contem(1, 0));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 4)]
    [InlineData(-1, 1)]
    public void CapacidadeBancoDe_limita_entre_1_e_4_abas(int abasPedidas, int abasEsperadas)
    {
        var capacidade = RegrasBanco.CapacidadeBancoDe(abasPedidas);
        Assert.Equal(abasEsperadas, capacidade.Abas);
        Assert.Equal(40, capacidade.CasasPorAba);
        Assert.Equal(abasEsperadas * 40, capacidade.Total);
    }

    [Fact]
    public void Constantes_de_expansao_e_custo_estao_definidas()
    {
        Assert.Equal(1, RegrasBanco.AbasBancoInicial);
        Assert.Equal(40, RegrasBanco.CasasPorAbaBanco);
        Assert.Equal(4, RegrasBanco.AbasBancoMaximas);
        Assert.Equal(500, RegrasBanco.CustoExpansaoBancoCoroas);
    }
}
