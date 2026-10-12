using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class SelecaoDeAlvosTestes
{
    private sealed class GrupoFalso(params long[] membros) : IAliadosDeCombate
    {
        public bool SaoAliados(long a, long b) =>
            a != b && membros.Contains(a) && membros.Contains(b);
    }

    private static AlvoDeCombate Jogador(long id) => new(id, 0, 0, 0.5, 100, 100, id);

    private static AlvoDeCombate Inimigo(long id) => new(id, 0, 0, 0.5, 100, 100);

    [Fact]
    public void Inimigo_e_boneco_sao_sempre_alvos_mesmo_com_grupo()
    {
        var grupo = new GrupoFalso(1, 2);
        Assert.True(SelecaoDeAlvos.PodeAcertar(1, Inimigo(-1), grupo));
        Assert.True(SelecaoDeAlvos.PodeAcertar(1, Inimigo(500), grupo));
    }

    [Fact]
    public void Aliado_do_grupo_nao_leva_o_golpe()
    {
        Assert.False(SelecaoDeAlvos.PodeAcertar(1, Jogador(2), new GrupoFalso(1, 2, 3)));
    }

    [Fact]
    public void O_proprio_atacante_nao_leva_o_golpe()
    {
        Assert.False(SelecaoDeAlvos.PodeAcertar(1, Jogador(1), new NinguemEAliado()));
    }

    [Fact]
    public void Jogador_de_fora_do_grupo_continua_alvo()
    {
        Assert.True(SelecaoDeAlvos.PodeAcertar(1, Jogador(9), new GrupoFalso(1, 2)));
    }

    [Fact]
    public void Sem_grupo_publicado_ninguem_e_aliado()
    {
        Assert.True(SelecaoDeAlvos.PodeAcertar(1, Jogador(2), new NinguemEAliado()));
        Assert.False(new NinguemEAliado().SaoAliados(1, 2));
    }
}
