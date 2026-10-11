using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasQuedaTestes
{
    [Fact]
    public void Tempos_da_proposta()
    {
        Assert.Equal(45_000, RegrasQueda.DuracaoCaidoMs);
        Assert.Equal(3_000, RegrasQueda.TempoParaLevantarMs);
        Assert.Equal(5_000, RegrasQueda.MinimoParaVoltarMs);
        Assert.Equal(2.0, RegrasQueda.AlcanceParaLevantarMetros);
        Assert.Equal(0.20, RegrasQueda.FracaoDeVidaAoLevantar);
    }

    [Theory]
    [InlineData(500, 100)]
    [InlineData(510, 102)]
    [InlineData(503, 101)] // 100,6 arredonda para cima
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    public void Vida_ao_levantar_e_20_por_cento_para_cima_e_nunca_zero(int maxima, int esperada) =>
        Assert.Equal(esperada, RegrasQueda.VidaAoLevantar(maxima));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(1.99, true)]
    [InlineData(2.0, true)]
    [InlineData(2.01, false)]
    [InlineData(-0.1, false)]
    public void Alcance_de_2_metros_inclui_o_limite(double distancia, bool vale) =>
        Assert.Equal(vale, RegrasQueda.NoAlcanceParaLevantar(distancia));

    [Theory]
    [InlineData(0, false)]
    [InlineData(4_900, false)]
    [InlineData(4_999, false)]
    [InlineData(5_000, true)]
    [InlineData(44_000, true)]
    public void Voltar_so_depois_de_5_segundos(long msCaido, bool pode) =>
        Assert.Equal(pode, RegrasQueda.PodeVoltarParaACidade(msCaido));

    [Theory]
    [InlineData(45_000, 45)]
    [InlineData(44_001, 45)]
    [InlineData(44_000, 44)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    public void Segundos_restantes_arredondam_para_cima(long ms, int esperado) =>
        Assert.Equal(esperado, RegrasQueda.SegundosRestantes(ms));
}
