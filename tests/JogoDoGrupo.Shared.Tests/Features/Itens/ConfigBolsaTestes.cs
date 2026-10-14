using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class ConfigBolsaTestes
{
    [Fact]
    public void Limite_maximo_de_abas_e_quatro()
    {
        Assert.Equal(4, ConfigBolsa.AbasMaximas);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(3, 250)]
    [InlineData(4, 500)]
    [InlineData(5, 0)]
    [InlineData(0, 0)]
    public void CustoPorAba_retorna_custo_progressivo_em_coroas(int abaDestino, long custoEsperado)
    {
        Assert.Equal(custoEsperado, ConfigBolsa.CustoPorAba(abaDestino));
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 250)]
    [InlineData(3, 500)]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    [InlineData(0, 0)]
    public void CustoProximaAba_retorna_custo_para_desbloquear_seguinte(
        int abasAtuais,
        long custoEsperado
    )
    {
        Assert.Equal(custoEsperado, ConfigBolsa.CustoProximaAba(abasAtuais));
    }

    [Fact]
    public void Rotas_contem_caminho_de_expandir()
    {
        Assert.Equal("/api/personagens/{personagemId}/inventario/expandir", RotasItens.Expandir);
        Assert.Equal("/api/personagens/99/inventario/expandir", RotasItens.ExpandirDe(99));
    }

    [Fact]
    public void Codigo_erro_bolsa_no_limite_definido()
    {
        Assert.Equal("BolsaNoLimite", CodigosErroItens.BolsaNoLimite);
    }
}
