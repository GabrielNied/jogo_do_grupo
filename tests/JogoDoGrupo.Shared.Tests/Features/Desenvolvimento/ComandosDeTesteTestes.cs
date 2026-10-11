using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Shared.Tests.Features.Desenvolvimento;

public class ComandosDeTesteTestes
{
    [Fact]
    public void Faixa_comeca_em_15000()
    {
        Assert.Equal(15000, DesenvolvimentoMessageIds.Faixa.Inicio);
        Assert.Equal(15999, DesenvolvimentoMessageIds.Faixa.Fim);
        Assert.True(
            DesenvolvimentoMessageIds.Faixa.Contem(DesenvolvimentoMessageIds.PedirComandoDeTeste)
        );
        Assert.True(
            DesenvolvimentoMessageIds.Faixa.Contem(
                DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste
            )
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ajuda")]
    [InlineData("AJUDA")]
    [InlineData("help")]
    [InlineData("?")]
    public void Vazio_ou_ajuda_lista_os_comandos(string? texto)
    {
        var comando = ComandosDeTeste.Interpretar(texto!);
        Assert.Equal(TipoDeComandoDeTeste.Ajuda, comando.Tipo);
    }

    [Fact]
    public void Xp_le_positivo_e_negativo()
    {
        var ganho = ComandosDeTeste.Interpretar("xp 40");
        Assert.Equal(TipoDeComandoDeTeste.Experiencia, ganho.Tipo);
        Assert.Equal(40, ganho.Quantidade);

        var perda = ComandosDeTeste.Interpretar("  XP  -15 ");
        Assert.Equal(TipoDeComandoDeTeste.Experiencia, perda.Tipo);
        Assert.Equal(-15, perda.Quantidade);
    }

    [Theory]
    [InlineData("xp")]
    [InlineData("xp 0")]
    [InlineData("xp 1000001")]
    [InlineData("xp -1000001")]
    [InlineData("xp 1,5")]
    [InlineData("xp 10 extra")]
    public void Xp_invalido_nao_vira_comando(string texto)
    {
        Assert.Equal(TipoDeComandoDeTeste.Invalido, ComandosDeTeste.Interpretar(texto).Tipo);
    }

    [Fact]
    public void Tp_le_tres_numeros_com_ponto()
    {
        var comando = ComandosDeTeste.Interpretar("tp 1.5 0 -20");
        Assert.Equal(TipoDeComandoDeTeste.Teleporte, comando.Tipo);
        Assert.Equal(1.5f, comando.X);
        Assert.Equal(0f, comando.Y);
        Assert.Equal(-20f, comando.Z);
    }

    [Theory]
    [InlineData("tp 1 2")]
    [InlineData("tp a 0 0")]
    [InlineData("spawn 1")]
    [InlineData("voar")]
    public void Texto_fora_do_formato_e_invalido(string texto)
    {
        var comando = ComandosDeTeste.Interpretar(texto);
        Assert.Equal(TipoDeComandoDeTeste.Invalido, comando.Tipo);
        Assert.False(string.IsNullOrEmpty(comando.Aviso));
    }

    [Fact]
    public void Spawn_nao_leva_numero()
    {
        Assert.Equal(TipoDeComandoDeTeste.Spawn, ComandosDeTeste.Interpretar("spawn").Tipo);
    }

    [Fact]
    public void Texto_longo_demais_e_recusado()
    {
        var comando = ComandosDeTeste.Interpretar(new string('x', ComandosDeTeste.TextoMaximo + 1));
        Assert.Equal(TipoDeComandoDeTeste.Invalido, comando.Tipo);
    }
}
