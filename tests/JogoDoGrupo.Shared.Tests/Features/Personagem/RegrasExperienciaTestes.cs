using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class RegrasExperienciaTestes
{
    [Theory]
    [InlineData(1, 50)]
    [InlineData(2, 115)]
    [InlineData(10, 792)]
    [InlineData(29, 2843)]
    [InlineData(30, 2962)]
    public void Curva_confere_a_tabela(int nivel, long esperado) =>
        Assert.Equal(esperado, RegrasExperiencia.XpParaOProximoNivel(nivel));

    [Fact]
    public void Ganho_sobe_dois_niveis_e_preserva_resto()
    {
        var resultado = RegrasExperiencia.AplicarGanho(1, 25, 190);
        Assert.Equal(3, resultado.Nivel);
        Assert.Equal(50, resultado.Xp);
        Assert.Equal(2 * ConfigExperiencia.PontosPorNivel, resultado.PontosGanhos);
    }

    [Theory]
    [InlineData(49, 1, 49, 0)]
    [InlineData(50, 2, 0, 1)]
    [InlineData(51, 2, 1, 1)]
    public void Ganho_respeita_limiar_exato(long ganho, int nivel, long xp, int niveisSubidos)
    {
        var resultado = RegrasExperiencia.AplicarGanho(1, 0, ganho);
        Assert.Equal(nivel, resultado.Nivel);
        Assert.Equal(xp, resultado.Xp);
        Assert.Equal(niveisSubidos * ConfigExperiencia.PontosPorNivel, resultado.PontosGanhos);
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

    [Fact]
    public void Ajuste_positivo_sobe_nivel_como_o_ganho()
    {
        var custo = RegrasExperiencia.XpParaOProximoNivel(1);
        var ajuste = RegrasExperiencia.AplicarAjuste(1, 0, 0, custo);
        Assert.Equal(2, ajuste.Nivel);
        Assert.Equal(0, ajuste.Xp);
        Assert.Equal(RegrasExperiencia.PontosPorNivel, ajuste.PontosLivres);
    }

    [Fact]
    public void Ajuste_negativo_desce_um_nivel_e_tira_so_os_pontos_daquele_nivel()
    {
        var custo = RegrasExperiencia.XpParaOProximoNivel(1);
        var desceu = RegrasExperiencia.AplicarAjuste(
            2,
            0,
            RegrasExperiencia.PontosPorNivel + 3,
            -1
        );
        Assert.Equal(1, desceu.Nivel);
        Assert.Equal(custo - 1, desceu.Xp);
        Assert.Equal(3, desceu.PontosLivres);
    }

    [Fact]
    public void Ajuste_negativo_no_nivel_1_zera_a_experiencia_e_nao_passa_de_zero_nos_pontos()
    {
        var ajuste = RegrasExperiencia.AplicarAjuste(1, 5, 0, -100);
        Assert.Equal(1, ajuste.Nivel);
        Assert.Equal(0, ajuste.Xp);
        Assert.Equal(0, ajuste.PontosLivres);
    }

    [Fact]
    public void Ajuste_zero_preserva_o_estado()
    {
        var ajuste = RegrasExperiencia.AplicarAjuste(4, 12, 7, 0);
        Assert.Equal(4, ajuste.Nivel);
        Assert.Equal(12, ajuste.Xp);
        Assert.Equal(7, ajuste.PontosLivres);
    }
}
