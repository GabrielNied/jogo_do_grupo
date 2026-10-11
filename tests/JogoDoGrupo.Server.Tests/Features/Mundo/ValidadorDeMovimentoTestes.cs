using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;

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
    [InlineData(1501f, 0, 0, 0)]
    [InlineData(0, 0, -1501f, 0)]
    [InlineData(0, -6f, 0, 0)]
    [InlineData(0, 91f, 0, 0)]
    [InlineData(0, 400f, 0, 0)]
    public void Valores_invalidos_ou_fora_do_terreno_sao_recusados(
        float x,
        float y,
        float z,
        float rot
    ) => Assert.False(ValidadorDeMovimento.Permitido(Origem, new(x, y, z, rot), 1000));

    // Substitui "Andar_pelo_terreno_de_1_km_e_permitido" (2026-10-11): o mundo passou a 3 km e
    // o chão ganhou relevo, então cada ponto é conferido na altura do chão dele.
    [Theory]
    [InlineData(60f, 63f)]
    [InlineData(-120f, -123f)]
    [InlineData(497f, 500f)]
    [InlineData(-497f, -500f)]
    [InlineData(900f, 903f)]
    [InlineData(-1200f, -1203f)]
    [InlineData(1497f, 1500f)]
    [InlineData(-1497f, -1500f)]
    public void Andar_pelo_mundo_de_3_km_e_permitido(float de, float para)
    {
        Assert.True(ValidadorDeMovimento.Permitido(NoChao(de, 0), NoChao(para, 0), 1000));
        Assert.True(ValidadorDeMovimento.Permitido(NoChao(0, de), NoChao(0, para), 1000));
    }

    [Fact]
    public void Passo_curto_para_fora_da_borda_e_recusado() =>
        Assert.False(ValidadorDeMovimento.Permitido(NoChao(1500f, 0), NoChao(1500.5f, 0), 1000));

    [Theory]
    [InlineData(0f, 1200f)]
    [InlineData(300f, 1350f)]
    [InlineData(-200f, -1100f)]
    [InlineData(-1100f, 200f)]
    public void Altura_e_conferida_contra_o_chao_do_ponto(float x, float z)
    {
        var chao = MapaDoMundo.AlturaEm(x, z);
        var antes = new PosicaoNaCidade(x, chao, z, 0);

        Assert.True(Permitido(antes, chao));
        Assert.True(Permitido(antes, chao + 1.5f), "Pulo é permitido.");
        Assert.True(Permitido(antes, chao + ValidadorDeMovimento.FolgaAcimaDoChao - 0.5f));
        Assert.True(Permitido(antes, chao - ValidadorDeMovimento.FolgaAbaixoDoChao + 0.5f));
        Assert.False(Permitido(antes, chao + ValidadorDeMovimento.FolgaAcimaDoChao + 0.5f));
        Assert.False(Permitido(antes, chao - ValidadorDeMovimento.FolgaAbaixoDoChao - 0.5f));
    }

    [Fact]
    public void Altura_zero_embaixo_de_uma_montanha_e_recusada()
    {
        // Um ponto alto das montanhas de neve: ficar em Y = 0 ali é estar dentro da pedra.
        var (x, z) = PontoAltoDaNeve();
        var chao = MapaDoMundo.AlturaEm(x, z);
        Assert.True(chao > 40f, $"Esperava montanha em ({x}, {z}); o chão está em {chao}.");

        var antes = new PosicaoNaCidade(x, chao, z, 0);
        Assert.False(ValidadorDeMovimento.Permitido(antes, new(x, 0f, z, 0), 1000));
        Assert.True(ValidadorDeMovimento.Permitido(antes, antes, 1000));
    }

    [Fact]
    public void Faixa_de_altura_cobre_todo_o_relevo()
    {
        Assert.Equal(MapaDoMundo.Metade, ValidadorDeMovimento.LimiteHorizontal);
        Assert.True(ValidadorDeMovimento.AlturaMinima < MapaDoMundo.AlturaMinima);
        Assert.True(ValidadorDeMovimento.AlturaMaxima > MapaDoMundo.AlturaMaxima);
    }

    private static PosicaoNaCidade NoChao(float x, float z) =>
        new(x, MapaDoMundo.AlturaEm(x, z), z, 0);

    private static bool Permitido(PosicaoNaCidade antes, float altura) =>
        ValidadorDeMovimento.Permitido(antes, antes with { Y = altura }, 1000);

    private static (float X, float Z) PontoAltoDaNeve()
    {
        var melhor = (X: 0f, Z: 1200f);
        var maior = float.MinValue;
        for (var z = 900f; z <= 1400f; z += 50f)
        {
            for (var x = -400f; x <= 400f; x += 50f)
            {
                var altura = MapaDoMundo.AlturaEm(x, z);
                if (altura > maior)
                {
                    maior = altura;
                    melhor = (x, z);
                }
            }
        }

        return melhor;
    }

    [Fact]
    public void Tempo_negativo_nao_da_folga_extra() =>
        Assert.False(ValidadorDeMovimento.Permitido(Origem, new(2f, 0, 0, 0), -1000));
}
