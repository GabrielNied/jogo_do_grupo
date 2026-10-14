using JogoDoGrupo.Shared.Features.Combate;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasEfeitosDeStatusTestes
{
    [Fact]
    public void Lentidao_reduz_velocidade_em_30_por_cento()
    {
        var semLentidao = RegrasEfeitosDeStatus.ModificadorVelocidade(false);
        var comLentidao = RegrasEfeitosDeStatus.ModificadorVelocidade(true);

        Assert.Equal(1.0, semLentidao);
        Assert.Equal(0.70, comLentidao, precision: 2);
    }

    [Fact]
    public void Vulneravel_aumenta_dano_recebido_em_15_por_cento()
    {
        var normal = RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(false);
        var vulneravel = RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(true);

        Assert.Equal(1.0, normal);
        Assert.Equal(1.15, vulneravel, precision: 2);
    }

    [Fact]
    public void Fortalecido_aumenta_dano_causado_em_15_por_cento()
    {
        var normal = RegrasEfeitosDeStatus.MultiplicadorDanoCausado(false);
        var fortalecido = RegrasEfeitosDeStatus.MultiplicadorDanoCausado(true);

        Assert.Equal(1.0, normal);
        Assert.Equal(1.15, fortalecido, precision: 2);
    }

    [Theory]
    [InlineData(TipoEfeitoDeStatus.Sangramento, true, 3)]
    [InlineData(TipoEfeitoDeStatus.Queimadura, true, 3)]
    [InlineData(TipoEfeitoDeStatus.Lentidao, false, 1)]
    [InlineData(TipoEfeitoDeStatus.Atordoado, false, 1)]
    [InlineData(TipoEfeitoDeStatus.Vulneravel, false, 1)]
    [InlineData(TipoEfeitoDeStatus.Fortalecido, false, 1)]
    public void Regras_de_acumulo_por_tipo(
        TipoEfeitoDeStatus tipo,
        bool podeAcumular,
        int maxAcumulos
    )
    {
        Assert.Equal(podeAcumular, RegrasEfeitosDeStatus.PodeAcumular(tipo));
        Assert.Equal(maxAcumulos, RegrasEfeitosDeStatus.MaximoAcumulos(tipo));
    }

    [Theory]
    [InlineData(TipoEfeitoDeStatus.Sangramento, 1, 5)]
    [InlineData(TipoEfeitoDeStatus.Sangramento, 2, 10)]
    [InlineData(TipoEfeitoDeStatus.Sangramento, 3, 15)]
    [InlineData(TipoEfeitoDeStatus.Sangramento, 4, 15)] // Teto de 3
    [InlineData(TipoEfeitoDeStatus.Queimadura, 1, 8)]
    [InlineData(TipoEfeitoDeStatus.Queimadura, 2, 16)]
    [InlineData(TipoEfeitoDeStatus.Queimadura, 3, 24)]
    [InlineData(TipoEfeitoDeStatus.Lentidao, 1, 0)]
    public void Dano_DoT_multiplica_pelos_acumulos(
        TipoEfeitoDeStatus tipo,
        int acumulos,
        int danoEsperado
    )
    {
        var dano = RegrasEfeitosDeStatus.CalcularDanoDoT(tipo, acumulos);
        Assert.Equal(danoEsperado, dano);
    }

    [Fact]
    public void ProcessarQuebraBoss_sobe_valor_ate_completar_barra()
    {
        RegrasEfeitosDeStatus.ProcessarQuebraBoss(0, 25, out var v1, out var quebrou1);
        Assert.Equal(25, v1);
        Assert.False(quebrou1);

        RegrasEfeitosDeStatus.ProcessarQuebraBoss(v1, 25, out var v2, out var quebrou2);
        Assert.Equal(50, v2);
        Assert.False(quebrou2);

        RegrasEfeitosDeStatus.ProcessarQuebraBoss(v2, 50, out var v3, out var quebrou3);
        Assert.Equal(0, v3);
        Assert.True(quebrou3);
    }

    [Fact]
    public void Duracoes_padrao_sao_positivas()
    {
        foreach (TipoEfeitoDeStatus tipo in Enum.GetValues(typeof(TipoEfeitoDeStatus)))
        {
            Assert.True(RegrasEfeitosDeStatus.DuracaoPadraoSegundos(tipo) > 0);
            Assert.False(string.IsNullOrWhiteSpace(RegrasEfeitosDeStatus.NomeDoEfeito(tipo)));
        }
    }
}
