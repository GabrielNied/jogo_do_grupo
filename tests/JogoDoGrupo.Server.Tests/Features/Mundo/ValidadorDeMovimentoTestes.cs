using JogoDoGrupo.Server.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class ValidadorDeMovimentoTestes
{
    private static readonly PosicaoNaCidade Origem = new(0, 0, 0, 0);

    [Fact]
    public void Nasce_no_ponto_do_hub() =>
        Assert.Equal(new PosicaoNaCidade(0, 0, -24, 0), ValidadorDeMovimento.Nascimento);

    [Fact]
    public void Parado_e_permitido() =>
        Assert.True(ValidadorDeMovimento.Permitido(Origem, Origem, 0));

    [Fact]
    public void Correr_e_permitido() =>
        Assert.True(ValidadorDeMovimento.Permitido(Origem, new(5f, 0, 0, 90), 1000));

    [Fact]
    public void Deslocamento_alem_de_correr_e_recusado() =>
        Assert.False(ValidadorDeMovimento.Permitido(Origem, new(20f, 0, 0, 0), 1000));

    [Fact]
    public void Teletransporte_logo_apos_o_ultimo_passo_e_recusado() =>
        Assert.False(ValidadorDeMovimento.Permitido(Origem, new(10f, 0, 0, 0), 50));

    [Fact]
    public void Tempo_parado_nao_acumula_alem_do_limite() =>
        Assert.False(ValidadorDeMovimento.Permitido(Origem, new(30f, 0, 0, 0), 60_000));

    [Fact]
    public void Altura_nao_conta_no_deslocamento() =>
        Assert.True(ValidadorDeMovimento.Permitido(Origem, new(0, 10f, 0, 0), 100));

    [Theory]
    [InlineData(float.NaN, 0, 0, 0)]
    [InlineData(0, float.PositiveInfinity, 0, 0)]
    [InlineData(0, 0, float.NaN, 0)]
    [InlineData(0, 0, 0, float.NaN)]
    [InlineData(501f, 0, 0, 0)]
    [InlineData(0, 0, -501f, 0)]
    [InlineData(0, -6f, 0, 0)]
    [InlineData(0, 51f, 0, 0)]
    public void Valores_invalidos_ou_fora_do_terreno_sao_recusados(
        float x,
        float y,
        float z,
        float rot
    ) => Assert.False(ValidadorDeMovimento.Permitido(Origem, new(x, y, z, rot), 1000));

    [Theory]
    [InlineData(60f, 63f)]
    [InlineData(-120f, -123f)]
    [InlineData(497f, 500f)]
    [InlineData(-497f, -500f)]
    public void Andar_pelo_terreno_de_1_km_e_permitido(float de, float para)
    {
        Assert.True(ValidadorDeMovimento.Permitido(new(de, 0, 0, 0), new(para, 0, 0, 0), 1000));
        Assert.True(ValidadorDeMovimento.Permitido(new(0, 0, de, 0), new(0, 0, para, 0), 1000));
    }

    [Fact]
    public void Passo_curto_para_fora_da_borda_e_recusado() =>
        Assert.False(
            ValidadorDeMovimento.Permitido(new(500f, 0, 0, 0), new(500.5f, 0, 0, 0), 1000)
        );

    [Fact]
    public void Tempo_negativo_nao_da_folga_extra() =>
        Assert.False(ValidadorDeMovimento.Permitido(Origem, new(2f, 0, 0, 0), -1000));
}
