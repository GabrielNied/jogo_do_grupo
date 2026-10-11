using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class RegrasExperienciaTestes
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 325)]
    [InlineData(10, 5012)]
    [InlineData(29, 30625)]
    [InlineData(30, 32442)]
    public void Curva_confere_tabela_provisoria(int nivel, long esperado) =>
        Assert.Equal(esperado, RegrasExperiencia.XpParaOProximoNivel(nivel));

    [Fact]
    public void Ganho_sobe_dois_niveis_e_preserva_resto()
    {
        var resultado = RegrasExperiencia.AplicarGanho(1, 25, 450);
        Assert.Equal(3, resultado.Nivel);
        Assert.Equal(50, resultado.Xp);
        Assert.Equal(10, resultado.PontosGanhos);
    }

    [Theory]
    [InlineData(99, 1, 99, 0)]
    [InlineData(100, 2, 0, 5)]
    [InlineData(101, 2, 1, 5)]
    public void Ganho_respeita_limiar_exato(long ganho, int nivel, long xp, int pontos)
    {
        var resultado = RegrasExperiencia.AplicarGanho(1, 0, ganho);
        Assert.Equal(nivel, resultado.Nivel);
        Assert.Equal(xp, resultado.Xp);
        Assert.Equal(pontos, resultado.PontosGanhos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void Ganho_nao_positivo_preserva_estado(long ganho)
    {
        var resultado = RegrasExperiencia.AplicarGanho(1, 500, ganho);
        Assert.Equal(1, resultado.Nivel);
        Assert.Equal(500, resultado.Xp);
        Assert.Equal(0, resultado.PontosGanhos);
    }

    [Fact]
    public void Ao_alcancar_teto_guarda_excedente_e_da_apenas_pontos_do_ultimo_nivel()
    {
        var resultado = RegrasExperiencia.AplicarGanho(
            ConfigExperiencia.NivelMaximo - 1,
            10,
            RegrasExperiencia.XpParaOProximoNivel(ConfigExperiencia.NivelMaximo - 1) + 20
        );
        Assert.Equal(ConfigExperiencia.NivelMaximo, resultado.Nivel);
        Assert.Equal(30, resultado.Xp);
        Assert.Equal(ConfigExperiencia.PontosPorNivel, resultado.PontosGanhos);
    }

    [Fact]
    public void No_teto_guarda_xp_sem_conceder_pontos()
    {
        var resultado = RegrasExperiencia.AplicarGanho(ConfigExperiencia.NivelMaximo, 100, 50000);
        Assert.Equal(ConfigExperiencia.NivelMaximo, resultado.Nivel);
        Assert.Equal(50100, resultado.Xp);
        Assert.Equal(0, resultado.PontosGanhos);
    }

    [Fact]
    public void Premio_grande_sobe_ate_teto_sem_perder_xp()
    {
        long custoTotal = 0;
        for (var nivel = 1; nivel < ConfigExperiencia.NivelMaximo; nivel++)
            custoTotal += RegrasExperiencia.XpParaOProximoNivel(nivel);
        var resultado = RegrasExperiencia.AplicarGanho(1, 0, long.MaxValue);
        Assert.Equal(ConfigExperiencia.NivelMaximo, resultado.Nivel);
        Assert.Equal(long.MaxValue - custoTotal, resultado.Xp);
        Assert.Equal(
            (ConfigExperiencia.NivelMaximo - 1) * ConfigExperiencia.PontosPorNivel,
            resultado.PontosGanhos
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ConfigExperiencia.NivelMaximo + 1)]
    public void Nivel_fora_da_faixa_e_recusado(int nivel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RegrasExperiencia.XpParaOProximoNivel(nivel)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RegrasExperiencia.AplicarGanho(nivel, 0, 1)
        );
    }

    [Fact]
    public void Xp_negativo_e_recusado() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RegrasExperiencia.AplicarGanho(1, -1, 0));

    [Fact]
    public void Soma_que_excede_long_e_recusada() =>
        Assert.Throws<OverflowException>(() =>
            RegrasExperiencia.AplicarGanho(ConfigExperiencia.NivelMaximo, long.MaxValue, 1)
        );
}
