using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class ConfigClimaTestes
{
    [Fact]
    public void Constantes_possuem_valores_definidos()
    {
        Assert.Equal(1.0f, ConfigClima.FatorVelocidadePadrao);
        Assert.Equal(0.70f, ConfigClima.FatorVelocidadeNevasca);
        Assert.Equal(500f, ConfigClima.DistanciaVisaoLimpo);
        Assert.Equal(250f, ConfigClima.DistanciaVisaoChuva);
        Assert.Equal(80f, ConfigClima.DistanciaVisaoNeblina);
        Assert.Equal(60f, ConfigClima.DistanciaVisaoNevasca);
        Assert.True(ConfigClima.DuracaoPadraoClimaSegundos > 0);
    }

    [Theory]
    [InlineData(TipoDeClima.Limpo, Bioma.Campo, 1.0f)]
    [InlineData(TipoDeClima.Limpo, Bioma.Neve, 1.0f)]
    [InlineData(TipoDeClima.Chuva, Bioma.Campo, 1.0f)]
    [InlineData(TipoDeClima.Neblina, Bioma.Pantano, 1.0f)]
    [InlineData(TipoDeClima.Nevasca, Bioma.Campo, 1.0f)]
    [InlineData(TipoDeClima.Nevasca, Bioma.Deserto, 1.0f)]
    [InlineData(TipoDeClima.Nevasca, Bioma.Neve, 0.70f)]
    public void Fator_de_velocidade_so_reduz_durante_nevasca_na_neve(
        TipoDeClima clima,
        Bioma bioma,
        float esperado
    )
    {
        Assert.Equal(esperado, ConfigClima.ObterFatorVelocidade(clima, bioma));
    }

    [Theory]
    [InlineData(TipoDeClima.Limpo, Bioma.Campo, 500f)]
    [InlineData(TipoDeClima.Chuva, Bioma.Campo, 250f)]
    [InlineData(TipoDeClima.Chuva, Bioma.Mata, 250f)]
    [InlineData(TipoDeClima.Chuva, Bioma.Deserto, 500f)]
    [InlineData(TipoDeClima.Neblina, Bioma.Pantano, 80f)]
    [InlineData(TipoDeClima.Neblina, Bioma.Mata, 80f)]
    [InlineData(TipoDeClima.Neblina, Bioma.Neve, 500f)]
    [InlineData(TipoDeClima.Nevasca, Bioma.Neve, 60f)]
    [InlineData(TipoDeClima.Nevasca, Bioma.Campo, 500f)]
    public void Distancia_de_visao_varia_conforme_o_bioma_e_clima(
        TipoDeClima clima,
        Bioma bioma,
        float esperado
    )
    {
        Assert.Equal(esperado, ConfigClima.ObterDistanciaVisao(clima, bioma));
    }
}
