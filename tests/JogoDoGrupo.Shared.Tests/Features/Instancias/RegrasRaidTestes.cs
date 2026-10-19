using JogoDoGrupo.Shared.Features.Instancias;

namespace JogoDoGrupo.Shared.Tests.Features.Instancias;

public sealed class RegrasRaidTestes
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(10, true)]
    public void Grupo_acima_de_cinco_e_raid(int membros, bool esperado) =>
        Assert.Equal(esperado, RegrasRaid.EhRaid(membros));

    [Theory]
    [InlineData(0, false)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Raid_comporta_ate_dez(int membros, bool esperado) =>
        Assert.Equal(esperado, RegrasRaid.Cabe(membros));
}
