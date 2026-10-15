using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Mundo;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Core.Mundo;

public class HistoricoDePosicoesDoMundoTestes
{
    private static PosicaoNoMundo Pos(long id, float x) => new("cidade", x, 0f, (float)id, 0f);

    [Fact]
    public void Retorna_a_ultima_amostra_em_ou_antes_do_instante()
    {
        var historico = new HistoricoDePosicoesDoMundo();
        historico.Registrar(1, Pos(1, 0f), 100);
        historico.Registrar(1, Pos(1, 5f), 200);

        Assert.True(historico.TentarObterEm(1, 150, out var meio));
        Assert.Equal(0f, meio.X);
        Assert.True(historico.TentarObterEm(1, 200, out var fim));
        Assert.Equal(5f, fim.X);
    }

    [Fact]
    public void Descarta_amostras_mais_velhas_que_a_janela()
    {
        var historico = new HistoricoDePosicoesDoMundo();
        historico.Registrar(1, Pos(1, 0f), 0);
        historico.Registrar(1, Pos(1, 1f), RegrasCompensacaoDeLag.JanelaMs);
        historico.Registrar(1, Pos(1, 2f), RegrasCompensacaoDeLag.JanelaMs * 2);

        Assert.Equal(2, historico.QuantidadeDeAmostras(1));
        Assert.False(historico.TentarObterEm(1, 0, out _));
    }
}
