using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasTrocaDeArmaTestes
{
    [Fact]
    public void Ids_da_troca_tem_os_numeros_do_contrato_e_estao_na_faixa()
    {
        Assert.Equal(6050, CombateMessageIds.IntencaoDeTrocaDeArma);
        Assert.Equal(6051, CombateMessageIds.TrocaDeArmaIniciada);
        Assert.Equal(6052, CombateMessageIds.TrocaDeArmaRecusada);
        Assert.True(CombateMessageIds.Faixa.Contem(CombateMessageIds.TrocaDeArmaRecusada));
    }

    [Fact]
    public void A_troca_dura_0_4_segundos()
    {
        Assert.Equal(0.4, RegrasTrocaDeArma.DuracaoDaTrocaSegundos);
        Assert.Equal(400, RegrasTrocaDeArma.DuracaoMs);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void Conjunto_seguinte_alterna_entre_os_dois(int atual, int esperado)
    {
        Assert.Equal(esperado, RegrasTrocaDeArma.ConjuntoSeguinte(atual));
    }
}
