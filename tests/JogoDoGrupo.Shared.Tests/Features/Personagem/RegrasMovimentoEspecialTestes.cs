using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class RegrasMovimentoEspecialTestes
{
    private static readonly VolumeMarcado Lago = new(
        "lago",
        TipoDeVolume.Agua,
        10f,
        -1f,
        10f,
        8f,
        4f,
        8f
    );

    private static readonly VolumeMarcado Parede = new(
        "parede",
        TipoDeVolume.ViaDeEscalada,
        0f,
        5f,
        0f,
        4f,
        10f,
        2f,
        rotacaoY: 90f
    );

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(4f, 0)]
    [InlineData(5f, 40)]
    [InlineData(10f, 240)]
    [InlineData(100f, ConfigMovimentoEspecial.DanoMaximoDeQueda)]
    public void Dano_da_queda_so_passa_da_altura_segura(float altura, int esperado) =>
        Assert.Equal(esperado, RegrasMovimentoEspecial.DanoDaQueda(altura));

    [Fact]
    public void Pulo_nao_machuca()
    {
        Assert.Equal(0, RegrasMovimentoEspecial.DanoDaQueda(1.25f));
        Assert.Equal(0, RegrasMovimentoEspecial.DanoDaQueda(float.NaN));
    }

    [Fact]
    public void Rampa_de_45_graus_nao_e_queda_livre_mas_pular_do_penhasco_e()
    {
        Assert.False(RegrasMovimentoEspecial.EhQuedaLivre(0.5f, 0.5f));
        Assert.True(RegrasMovimentoEspecial.EhQuedaLivre(2f, 0.5f));
        Assert.True(RegrasMovimentoEspecial.EhQuedaLivre(3f, 0f));
    }

    [Fact]
    public void Nada_em_volume_de_agua_funda_com_os_pes_abaixo_da_superficie()
    {
        // Superfície em y = 1. Pés em 0,5 nadam; em 0,9 (acima da folga) e em 1,5 não.
        Assert.True(RegrasMovimentoEspecial.EstaNadando(10f, 0.5f, 10f, new[] { Lago }));
        Assert.False(RegrasMovimentoEspecial.EstaNadando(10f, 0.9f, 10f, new[] { Lago }));
        Assert.False(RegrasMovimentoEspecial.EstaNadando(10f, 1.5f, 10f, new[] { Lago }));
        Assert.False(RegrasMovimentoEspecial.EstaNadando(30f, 0.5f, 10f, new[] { Lago }));
    }

    [Fact]
    public void Agua_rasa_nao_da_para_nadar()
    {
        var poca = new VolumeMarcado("poca", TipoDeVolume.Agua, 0f, 0f, 0f, 4f, 1f, 4f);
        Assert.False(RegrasMovimentoEspecial.EstaNadando(0f, -0.4f, 0f, new[] { poca }));
    }

    [Fact]
    public void So_agua_funda_amortece_a_queda()
    {
        Assert.True(RegrasMovimentoEspecial.AguaAmortece(10f, 0f, 10f, new[] { Lago }));
        var rasa = new VolumeMarcado("rasa", TipoDeVolume.Agua, 0f, 0f, 0f, 4f, 1.5f, 4f);
        Assert.False(RegrasMovimentoEspecial.AguaAmortece(0f, 0f, 0f, new[] { rasa }));
    }

    [Fact]
    public void Escalada_so_vale_dentro_do_volume_marcado_com_a_margem()
    {
        // Parede girada 90 graus: o volume é largo em Z (4) e estreito em X (2).
        var dentro = RegrasMovimentoEspecial.ProcurarVia(0.5f, 3f, 1.5f, new[] { Parede });
        Assert.Equal("parede", dentro.Id);

        Assert.Null(RegrasMovimentoEspecial.ProcurarVia(3f, 3f, 0f, new[] { Parede }));
        Assert.Null(RegrasMovimentoEspecial.ProcurarVia(0f, 11f, 0f, new[] { Parede }));
        Assert.Null(
            RegrasMovimentoEspecial.ProcurarVia(0f, 3f, 0f, System.Array.Empty<VolumeMarcado>())
        );
    }

    [Fact]
    public void A_frente_do_volume_acompanha_o_giro()
    {
        Parede.Frente(out var x, out var z);
        Assert.Equal(1f, x, 3);
        Assert.Equal(0f, z, 3);
    }
}
