using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasCompensacaoDeLagTestes
{
    [Fact]
    public void Sem_referencia_usa_o_instante_atual()
    {
        Assert.Equal(10_000L, RegrasCompensacaoDeLag.InstanteParaTeste(0, 10_000));
        Assert.Equal(10_000L, RegrasCompensacaoDeLag.InstanteParaTeste(-5, 10_000));
    }

    [Fact]
    public void Dentro_da_janela_rebobina_ate_o_tempo_visto()
    {
        var agora = 10_000L;
        var visto = agora - 200;
        Assert.Equal(visto, RegrasCompensacaoDeLag.InstanteParaTeste(visto, agora));
    }

    [Fact]
    public void Fora_da_janela_usa_o_instante_atual()
    {
        var agora = 10_000L;
        Assert.Equal(agora, RegrasCompensacaoDeLag.InstanteParaTeste(agora - 251, agora));
    }

    [Fact]
    public void Tempo_visto_no_futuro_usa_o_instante_atual()
    {
        var agora = 10_000L;
        Assert.Equal(agora, RegrasCompensacaoDeLag.InstanteParaTeste(agora + 50, agora));
    }
}
