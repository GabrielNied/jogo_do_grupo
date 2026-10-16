using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Puxada em cadeia (#915): atacar um inimigo puxa os grupos da mesma espécie a até 10 m dele.
/// O jogador fica longe da visão de todos: só a cadeia pode alertar os vizinhos.
/// </summary>
public class PuxadaEmCadeiaTestes
{
    private const long Jogador = 9;
    private const double CentroZ = 190.0;
    private const long IdA = ConfigInimigosDoCampo.PrimeiroInimigoId;
    private const long IdB = ConfigInimigosDoCampo.PrimeiroInimigoId + 1_000;

    private sealed class Sorteador : ISorteadorDeCriaturas
    {
        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                definicao.VidaBase,
                definicao.DanoBase,
                definicao.ExperienciaBase,
                1.0,
                1.0f
            );

        public bool Aparece(DefinicaoDeCriatura definicao) => true;

        public double Proximo() => 0.0;
    }

    private sealed class SemQueda : IEstadoDeQueda
    {
        public bool EstaCaido(long personagemId) => false;
    }

    private long _agora = 10_000;

    private GrupoDeInimigos Grupo(int criatura, double x, long primeiroId) =>
        new(
            CatalogoCriaturas.Obter(criatura),
            new Sorteador(),
            1,
            () => _agora,
            pontoX: x,
            pontoZ: CentroZ,
            primeiroId: primeiroId,
            queda: new SemQueda()
        );

    private List<JogadorNoCampo> Longe() => [new(Jogador, 120.0, CentroZ)];

    private void Passos(int quantidade, params GrupoDeInimigos[] grupos)
    {
        for (var i = 0; i < quantidade; i++)
        {
            _agora += 100;
            foreach (var g in grupos)
            {
                g.Passo(Longe());
            }
        }
    }

    private static EstadoDeInimigo Estado(GrupoDeInimigos grupo) => grupo.Vivos()[0].Estado;

    private EstadoDeInimigo AtacarA(int criaturaDeB, double xDeB)
    {
        var a = Grupo(CatalogoCriaturas.JavaliDoCampo, 0.0, IdA);
        var b = Grupo(criaturaDeB, xDeB, IdB);
        PuxadaEmCadeia.Ligar([a, b]);
        Passos(2, a, b);
        Assert.Equal(EstadoDeInimigo.Repouso, Estado(b));

        Assert.True(a.TentarAplicarDano(IdA, 1, Jogador, out _));
        Passos(2, a, b);
        return Estado(b);
    }

    [Fact]
    public void Grupo_da_mesma_especie_a_9_virgula_9_m_entra_em_alerta_junto()
    {
        Assert.NotEqual(EstadoDeInimigo.Repouso, AtacarA(CatalogoCriaturas.JavaliDoCampo, 9.9));
    }

    [Fact]
    public void Grupo_da_mesma_especie_a_10_virgula_1_m_nao_entra()
    {
        Assert.Equal(EstadoDeInimigo.Repouso, AtacarA(CatalogoCriaturas.JavaliDoCampo, 10.1));
    }

    [Fact]
    public void Especie_diferente_perto_nao_entra()
    {
        Assert.Equal(EstadoDeInimigo.Repouso, AtacarA(CatalogoCriaturas.LoboDoCampo, 5.0));
    }

    [Fact]
    public void Sem_ataque_nenhum_grupo_e_chamado()
    {
        var a = Grupo(CatalogoCriaturas.JavaliDoCampo, 0.0, IdA);
        var b = Grupo(CatalogoCriaturas.JavaliDoCampo, 5.0, IdB);
        PuxadaEmCadeia.Ligar([a, b]);

        Passos(10, a, b);

        Assert.Equal(EstadoDeInimigo.Repouso, Estado(a));
        Assert.Equal(EstadoDeInimigo.Repouso, Estado(b));
    }
}
