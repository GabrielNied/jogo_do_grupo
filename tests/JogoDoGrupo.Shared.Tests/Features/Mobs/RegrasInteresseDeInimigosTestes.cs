using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasInteresseDeInimigosTestes
{
    [Theory]
    [InlineData(79, false, true)]
    [InlineData(81, false, false)]
    [InlineData(99, true, true)]
    [InlineData(101, true, false)]
    public void Histerese_de_envio(double distancia, bool jaRecebia, bool esperado) =>
        Assert.Equal(esperado, RegrasInteresseDeInimigos.DeveEnviar(distancia, jaRecebia));
}
