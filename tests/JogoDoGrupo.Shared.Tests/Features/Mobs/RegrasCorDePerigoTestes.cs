using JogoDoGrupo.Shared.Features.Mobs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasCorDePerigoTestes
{
    [Theory]
    [InlineData(10, 6, TomDePerigoDoNivel.Trivial)]
    [InlineData(10, 8, TomDePerigoDoNivel.Normal)]
    [InlineData(10, 10, TomDePerigoDoNivel.Normal)]
    [InlineData(10, 11, TomDePerigoDoNivel.Atencao)]
    [InlineData(10, 13, TomDePerigoDoNivel.Atencao)]
    [InlineData(10, 14, TomDePerigoDoNivel.Mortal)]
    public void Tom_respeita_limites(int jogador, int inimigo, TomDePerigoDoNivel esperado) =>
        Assert.Equal(esperado, RegrasCorDePerigo.Tom(jogador, inimigo));
}
