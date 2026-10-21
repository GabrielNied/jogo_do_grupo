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

    [Fact]
    public void Total_de_etapas_da_raid_e_tres()
    {
        Assert.Equal(3, RegrasRaid.TotalEtapas);
        Assert.Equal(1, RegrasRaid.PrimeiraEtapa);
        Assert.Equal(3, RegrasRaid.UltimaEtapa);
    }

    [Theory]
    [InlineData(1, 200)]
    [InlineData(2, 100)]
    [InlineData(3, 301)]
    public void ObterChefeDaEtapa_retorna_chefe_correto(int etapa, int chefeEsperado)
    {
        Assert.True(RegrasRaid.EtapaValida(etapa));
        Assert.Equal(chefeEsperado, RegrasRaid.ObterChefeDaEtapa(etapa));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void ObterChefeDaEtapa_com_etapa_invalida_lanca_excecao(int etapa)
    {
        Assert.False(RegrasRaid.EtapaValida(etapa));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegrasRaid.ObterChefeDaEtapa(etapa));
    }

    [Fact]
    public void ObterChaveSemana_muda_na_terca_as_seis_brt()
    {
        // 2026-10-20 é uma terça-feira.
        // 08:59 UTC = 05:59 BRT (antes do reset das 06:00 BRT) -> pertence à semana anterior (início 2026-10-13)
        var antesDoReset = new DateTimeOffset(2026, 10, 20, 8, 59, 0, TimeSpan.Zero);
        Assert.Equal("2026-10-13", RegrasRaid.ObterChaveSemana(antesDoReset));

        // 09:00 UTC = 06:00 BRT (exatamente no reset) -> inicia nova semana (2026-10-20)
        var noReset = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        Assert.Equal("2026-10-20", RegrasRaid.ObterChaveSemana(noReset));

        // 2026-10-21 (quarta-feira) 12:00 UTC -> mesma semana 2026-10-20
        var diaSeguinte = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("2026-10-20", RegrasRaid.ObterChaveSemana(diaSeguinte));
    }

    [Fact]
    public void JaColetouLootDaEtapa_verifica_corretamente()
    {
        var concluidas = new[] { 1, 2 };
        Assert.True(RegrasRaid.JaColetouLootDaEtapa(concluidas, 1));
        Assert.True(RegrasRaid.JaColetouLootDaEtapa(concluidas, 2));
        Assert.False(RegrasRaid.JaColetouLootDaEtapa(concluidas, 3));
        Assert.False(RegrasRaid.JaColetouLootDaEtapa(null, 1));
    }
}
