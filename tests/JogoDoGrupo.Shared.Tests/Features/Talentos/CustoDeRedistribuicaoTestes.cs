using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Talentos;

public class CustoDeRedistribuicaoTestes
{
    [Theory]
    [InlineData(1, 50)]
    [InlineData(10, 100)]
    [InlineData(20, 250)]
    [InlineData(50, 1300)]
    [InlineData(100, 5050)]
    public void Custo_acompanha_a_curva_proposta(int nivel, long esperado)
    {
        Assert.Equal(esperado, ConfigRedistribuicao.CustoEmCoroas(nivel));
    }

    [Fact]
    public void Custo_nunca_cai_quando_o_nivel_sobe()
    {
        var anterior = 0L;
        for (var nivel = 1; nivel <= ConfigExperiencia.NivelMaximo; nivel++)
        {
            var custo = ConfigRedistribuicao.CustoEmCoroas(nivel);
            Assert.True(custo >= anterior, "Nível " + nivel);
            Assert.Equal(0, custo % ConfigRedistribuicao.ArredondamentoCoroas);
            anterior = custo;
        }
    }

    [Fact]
    public void Nivel_fora_da_faixa_e_limitado()
    {
        Assert.Equal(ConfigRedistribuicao.CustoEmCoroas(1), ConfigRedistribuicao.CustoEmCoroas(0));
        Assert.Equal(ConfigRedistribuicao.CustoEmCoroas(1), ConfigRedistribuicao.CustoEmCoroas(-5));
        Assert.Equal(
            ConfigRedistribuicao.CustoEmCoroas(ConfigExperiencia.NivelMaximo),
            ConfigRedistribuicao.CustoEmCoroas(ConfigExperiencia.NivelMaximo + 50)
        );
    }
}
