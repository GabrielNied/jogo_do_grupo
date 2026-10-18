using JogoDoGrupo.Shared.Core.Moderacao;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Core;

public class RegrasModeracaoTestes
{
    [Theory]
    [InlineData("tp")]
    [InlineData("TP")]
    [InlineData("  tp  ")]
    [InlineData("puxar")]
    [InlineData("silenciar")]
    [InlineData("expulsar")]
    [InlineData("banir")]
    [InlineData("ajuda")]
    public void Comandos_validos_sao_reconhecidos(string comando)
    {
        Assert.True(RegrasModeracao.EhComandoGmValido(comando));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalido")]
    [InlineData("kill")]
    [InlineData("godmode")]
    public void Comandos_invalidos_sao_recusados(string? comando)
    {
        Assert.False(RegrasModeracao.EhComandoGmValido(comando));
    }

    [Theory]
    [InlineData("ofensa")]
    [InlineData("trapaca")]
    [InlineData("spam")]
    [InlineData("comportamento")]
    [InlineData("outro")]
    [InlineData("  OFENSA  ")]
    public void Motivos_denuncia_validos_sao_reconhecidos(string motivo)
    {
        Assert.True(RegrasModeracao.EhMotivoDenunciaValido(motivo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("motivo_aleatorio")]
    public void Motivos_denuncia_invalidos_sao_recusados(string? motivo)
    {
        Assert.False(RegrasModeracao.EhMotivoDenunciaValido(motivo));
    }

    [Fact]
    public void Normalizar_minutos_silencio_aplica_limites_e_padrao()
    {
        Assert.Equal(10, RegrasModeracao.NormalizarMinutosSilencio(0));
        Assert.Equal(10, RegrasModeracao.NormalizarMinutosSilencio(-5));
        Assert.Equal(30, RegrasModeracao.NormalizarMinutosSilencio(30));
        Assert.Equal(
            RegrasModeracao.MaximoMinutosSilencio,
            RegrasModeracao.NormalizarMinutosSilencio(999999)
        );
    }

    [Fact]
    public void Normalizar_dias_banimento_aplica_limites_e_padrao()
    {
        Assert.Equal(1, RegrasModeracao.NormalizarDiasBanimento(0));
        Assert.Equal(1, RegrasModeracao.NormalizarDiasBanimento(-10));
        Assert.Equal(7, RegrasModeracao.NormalizarDiasBanimento(7));
        Assert.Equal(
            RegrasModeracao.MaximoDiasBanimento,
            RegrasModeracao.NormalizarDiasBanimento(9999)
        );
    }
}
