using JogoDoGrupo.Shared.Features.Pvp;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Pvp;

public sealed class RegrasPvpTestes
{
    [Fact]
    public void Vitoria_na_arena_adiciona_25_pontos()
    {
        var inicial = 1000;
        var novo = RegrasPvp.CalcularPontosAposPartida(inicial, vitoria: true);
        Assert.Equal(1025, novo);
    }

    [Fact]
    public void Derrota_na_arena_subtrai_15_pontos_sem_ficar_negativo()
    {
        var inicial = 1000;
        var novo = RegrasPvp.CalcularPontosAposPartida(inicial, vitoria: false);
        Assert.Equal(985, novo);

        // Pontuação não fica negativa
        var zero = RegrasPvp.CalcularPontosAposPartida(10, vitoria: false);
        Assert.Equal(0, zero);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 10, 0, true)]
    [InlineData(0, 0, 15, 0, true)]
    [InlineData(0, 0, 15.1, 0, false)]
    [InlineData(0, 0, 20, 20, false)]
    public void Distancia_valida_para_convite_respeita_limite_de_15_metros(
        double x1,
        double z1,
        double x2,
        double z2,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasPvp.DistanciaValidaParaConvite(x1, z1, x2, z2));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 49, 0, true)]
    [InlineData(0, 0, 50, 0, true)]
    [InlineData(0, 0, 50.1, 0, false)]
    [InlineData(0, 0, 36, 36, false)] // sqrt(36^2 + 36^2) ≈ 50.9 > 50
    public void Esta_no_raio_do_duelo_respeita_limite_de_50_metros(
        double centroX,
        double centroZ,
        double jogadorX,
        double jogadorZ,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasPvp.EstaNoRaioDoDuelo(centroX, centroZ, jogadorX, jogadorZ));
    }
}
