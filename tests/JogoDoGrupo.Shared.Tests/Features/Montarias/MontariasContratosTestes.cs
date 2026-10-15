using JogoDoGrupo.Shared.Features.Montarias;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Montarias;

public class MontariasContratosTestes
{
    [Fact]
    public void Faixa_comeca_em_16000()
    {
        Assert.Equal(16000, MontariasMessageIds.Faixa.Inicio);
        Assert.Equal(16999, MontariasMessageIds.Faixa.Fim);
    }

    [Fact]
    public void Velocidade_da_montaria_e_maior_que_corrida_padrao()
    {
        Assert.True(RegrasMontaria.VelocidadeMontariaPadrao > 5.5f);
        Assert.Equal(9.0f, RegrasMontaria.VelocidadeMontariaPadrao);
    }
}
