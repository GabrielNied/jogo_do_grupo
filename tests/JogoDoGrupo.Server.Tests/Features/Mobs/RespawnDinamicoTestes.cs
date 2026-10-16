using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>Respawn dinâmico pela lotação (#897): mais jogadores perto, reposição mais curta.</summary>
public class RespawnDinamicoTestes
{
    private const long IdA = ConfigInimigosDoCampo.PrimeiroInimigoId;
    private const double CentroZ = 190.0;
    private static readonly double Base = CatalogoCriaturas
        .Obter(CatalogoCriaturas.JavaliDoCampo)
        .IntervaloDeAparicaoSegundos;

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

    [Fact]
    public void Fracao_sem_jogadores_e_1_e_cai_ate_o_limite()
    {
        Assert.Equal(1.0, ConfigRespawn.Fracao(0));
        Assert.True(ConfigRespawn.Fracao(1) < 1.0);
        Assert.True(ConfigRespawn.Fracao(3) < ConfigRespawn.Fracao(1));
        Assert.Equal(ConfigRespawn.FracaoMinimaDoIntervalo, ConfigRespawn.Fracao(50));
        Assert.Equal(ConfigRespawn.Fracao(50), ConfigRespawn.Fracao(500));
    }

    // Mata um dos dois inimigos com 'jogadores' de pé perto (e fora da visão) e devolve em
    // quantos segundos o grupo volta a ter dois.
    private static double SegundosAteVoltar(
        int jogadores,
        LimiteDeInimigosVivos? limite = null,
        Action<GrupoDeInimigos>? aoMatar = null
    )
    {
        var agora = 10_000L;
        var grupo = new GrupoDeInimigos(
            CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliDoCampo),
            new Sorteador(),
            2,
            () => agora,
            pontoX: 0.0,
            pontoZ: CentroZ,
            primeiroId: IdA,
            limite: limite,
            queda: new SemQueda()
        );
        var gente = Enumerable
            .Range(0, jogadores)
            .Select(i => new JogadorNoCampo(100 + i, 40.0, CentroZ))
            .ToList();
        agora += 100;
        grupo.Passo(gente);

        Assert.True(grupo.TentarAplicarDano(IdA, 100_000, 100, out _));
        aoMatar?.Invoke(grupo);
        var inicio = agora;
        for (var i = 0; i < 20_000; i++)
        {
            agora += 100;
            grupo.Passo(gente);
            if (grupo.Vivos().Length == 2)
            {
                return (agora - inicio) / 1000.0;
            }
        }

        return double.PositiveInfinity;
    }

    [Fact]
    public void Zero_jogadores_perto_usa_o_intervalo_base()
    {
        Assert.InRange(SegundosAteVoltar(0), Base, Base + 0.5);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Com_jogadores_o_intervalo_encurta(int jogadores)
    {
        var esperado = Base * ConfigRespawn.Fracao(jogadores);
        Assert.InRange(SegundosAteVoltar(jogadores), esperado, esperado + 0.5);
    }

    [Fact]
    public void Lotacao_enorme_nunca_passa_do_limite_da_curva()
    {
        var minimo = Base * ConfigRespawn.FracaoMinimaDoIntervalo;
        Assert.InRange(SegundosAteVoltar(40), minimo, minimo + 0.5);
    }

    [Fact]
    public void Jogador_longe_do_ponto_nao_conta()
    {
        var agora = 10_000L;
        var grupo = new GrupoDeInimigos(
            CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliDoCampo),
            new Sorteador(),
            2,
            () => agora,
            pontoX: 0.0,
            pontoZ: CentroZ,
            primeiroId: IdA,
            queda: new SemQueda()
        );
        var longe = new List<JogadorNoCampo>
        {
            new(100, ConfigRespawn.RaioDaLotacaoMetros + 40.0, CentroZ),
        };
        agora += 100;
        grupo.Passo(longe);
        Assert.True(grupo.TentarAplicarDano(IdA, 100_000, 100, out _));
        var inicio = agora;
        while (grupo.Vivos().Length < 2 && agora - inicio < 300_000)
        {
            agora += 100;
            grupo.Passo(longe);
        }

        Assert.InRange((agora - inicio) / 1000.0, Base, Base + 0.5);
    }

    [Fact]
    public void Sem_vaga_no_teto_de_vivos_nao_repoe_mesmo_com_lotacao_alta()
    {
        var limite = new LimiteDeInimigosVivos(2);
        // Alguém toma a vaga que a morte liberou: o teto de vivos continua mandando.
        var segundos = SegundosAteVoltar(10, limite, _ => Assert.Equal(1, limite.Reservar(1)));

        Assert.True(double.IsPositiveInfinity(segundos));
        Assert.True(limite.Vivos <= limite.Maximo);
    }
}
