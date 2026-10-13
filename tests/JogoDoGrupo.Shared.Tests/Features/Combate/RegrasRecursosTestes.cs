using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasRecursosTestes
{
    [Fact]
    public void ValorFinal_soma_as_quatro_fontes() =>
        Assert.Equal(10, RegrasRecursos.ValorFinal(5, 0, 3, 2));

    [Theory]
    [InlineData(0, 500)]
    [InlineData(10, 600)]
    [InlineData(-5, 450)]
    public void VidaMaxima_e_500_mais_10_por_vigor(int vigor, int esperado) =>
        Assert.Equal(esperado, RegrasRecursos.VidaMaxima(vigor));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(10, 120)]
    public void StaminaMaxima_e_100_mais_2_por_agilidade(int agilidade, int esperado) =>
        Assert.Equal(esperado, RegrasRecursos.StaminaMaxima(agilidade));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(10, 120)]
    public void ManaMaxima_e_100_mais_2_por_espirito(int espirito, int esperado) =>
        Assert.Equal(esperado, RegrasRecursos.ManaMaxima(espirito));

    [Theory]
    [InlineData(50, 100, 50)]
    [InlineData(150, 100, 100)]
    [InlineData(-3, 100, 0)]
    [InlineData(10, -1, 0)]
    public void Limitar_mantem_entre_zero_e_maximo(int atual, int maximo, int esperado) =>
        Assert.Equal(esperado, RegrasRecursos.Limitar(atual, maximo));

    [Fact]
    public void Reduzir_maximo_nao_cura()
    {
        Assert.Equal(30, RegrasRecursos.Limitar(30, 80));
        Assert.Equal(80, RegrasRecursos.Limitar(100, 80));
    }

    [Fact]
    public void Stamina_nao_regenera_antes_dos_2_segundos()
    {
        Assert.Equal(50, RegrasRecursos.RegenerarStamina(50, 100, 1.5, 1.0));
        Assert.Equal(50, RegrasRecursos.RegenerarStamina(50, 100, 2.0, 1.0));
    }

    [Fact]
    public void Stamina_regenera_10_por_segundo_depois_do_atraso() =>
        Assert.Equal(60, RegrasRecursos.RegenerarStamina(50, 100, 3.0, 1.0));

    [Fact]
    public void Intervalo_que_cruza_o_atraso_conta_so_a_parte_depois() =>
        // 2,5 s desde o gasto, intervalo de 1 s: só 0,5 s regenera.
        Assert.Equal(55, RegrasRecursos.RegenerarStamina(50, 100, 2.5, 1.0));

    [Fact]
    public void Mana_regenera_5_por_segundo() =>
        Assert.Equal(70, RegrasRecursos.RegenerarMana(50, 120, 10.0, 4.0));

    [Fact]
    public void Regeneracao_nao_passa_do_maximo() =>
        Assert.Equal(100, RegrasRecursos.RegenerarStamina(95, 100, 10.0, 5.0));

    [Fact]
    public void Regeneracao_sem_tempo_nao_muda() =>
        Assert.Equal(40, RegrasRecursos.RegenerarStamina(40, 100, 10.0, 0.0));

    [Fact]
    public void Regeneracao_limita_atual_acima_do_maximo() =>
        Assert.Equal(80, RegrasRecursos.RegenerarStamina(100, 80, 10.0, 1.0));

    [Fact]
    public void Rota_e_codigo_de_erro_sao_os_combinados()
    {
        Assert.Equal("/api/personagens/{personagemId}/recursos", RotasCombate.Recursos);
        Assert.Equal("/api/personagens/7/recursos", RotasCombate.RecursosDe(7));
        Assert.Equal("PersonagemNaoEncontrado", CodigosErroCombate.PersonagemNaoEncontrado);
    }

    [Fact]
    public void Resposta_nasce_com_atributos_nao_nulos() =>
        Assert.NotNull(new RecursosResponse().Atributos);
}
