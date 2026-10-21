using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class SorteadorDeCriaturasTestes
{
    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    )!;
    private static readonly DefinicaoDeCriatura Chefe = CatalogoCriaturas.Obter(
        CatalogoCriaturas.GuardiaoDoReflexo
    )!;
    private static readonly DefinicaoDeCriatura Raro = CatalogoCriaturas.Obter(
        CatalogoCriaturas.PresaVelha
    )!;

    private static List<CriaturaSorteada> Sortear(
        DefinicaoDeCriatura definicao,
        int vezes,
        int semente
    )
    {
        var sorteador = new SorteadorDeCriaturas(new Random(semente));
        return Enumerable.Range(0, vezes).Select(_ => sorteador.Sortear(definicao)).ToList();
    }

    [Fact]
    public void Comum_vem_elite_perto_de_5_por_cento_das_vezes()
    {
        var criaturas = Sortear(Javali, 20000, semente: 1);

        var elites = criaturas.Count(c => c.Variante == VarianteDeCriatura.Elite);
        Assert.InRange(elites / 20000.0, 0.04, 0.06);
    }

    [Fact]
    public void Normal_sai_com_os_valores_de_base()
    {
        var criaturas = Sortear(Javali, 200, semente: 2);
        var normal = criaturas.First(c => c.Variante == VarianteDeCriatura.Normal);

        Assert.Same(Javali, normal.Definicao);
        Assert.Equal(180, normal.VidaMaxima);
        Assert.Equal(40, normal.Dano);
        Assert.Equal(20, normal.Experiencia);
        Assert.Equal(1.0, normal.MultiplicadorDeRecompensa);
        Assert.Equal(1f, normal.Escala);
    }

    [Fact]
    public void Elite_sai_mais_forte_maior_e_com_mais_recompensa()
    {
        var criaturas = Sortear(Javali, 2000, semente: 3);
        var elite = criaturas.First(c => c.Variante == VarianteDeCriatura.Elite);

        Assert.Equal(360, elite.VidaMaxima);
        Assert.Equal(60, elite.Dano);
        Assert.Equal(40, elite.Experiencia);
        Assert.Equal(2.0, elite.MultiplicadorDeRecompensa);
        Assert.Equal(1.3f, elite.Escala);
    }

    [Fact]
    public void Chefe_e_chefe_raro_nunca_saem_elites()
    {
        var chefes = Sortear(Chefe, 5000, semente: 4).Concat(Sortear(Raro, 5000, semente: 5));

        Assert.All(
            chefes,
            c =>
            {
                Assert.Equal(VarianteDeCriatura.Normal, c.Variante);
                Assert.Equal(c.Definicao.VidaBase, c.VidaMaxima);
                Assert.Equal(c.Definicao.DanoBase, c.Dano);
                Assert.Equal(c.Definicao.ExperienciaBase, c.Experiencia);
                Assert.Equal(1.0, c.MultiplicadorDeRecompensa);
                Assert.Equal(1f, c.Escala);
            }
        );
    }

    [Fact]
    public void Chefe_raro_aparece_perto_de_20_por_cento_dos_sorteios()
    {
        var sorteador = new SorteadorDeCriaturas(new Random(6));

        var apareceu = Enumerable.Range(0, 20000).Count(_ => sorteador.Aparece(Raro));

        Assert.InRange(apareceu / 20000.0, 0.18, 0.22);
    }

    [Fact]
    public void Comum_e_chefe_sempre_aparecem()
    {
        var sorteador = new SorteadorDeCriaturas(new Random(7));

        Assert.All(Enumerable.Range(0, 2000), _ => Assert.True(sorteador.Aparece(Javali)));
        Assert.All(Enumerable.Range(0, 2000), _ => Assert.True(sorteador.Aparece(Chefe)));
    }

    [Fact]
    public void Mesma_semente_da_o_mesmo_resultado()
    {
        Assert.Equal(
            Sortear(Javali, 500, semente: 8).Select(c => c.Variante),
            Sortear(Javali, 500, semente: 8).Select(c => c.Variante)
        );
    }

    [Fact]
    public async Task Sorteios_ao_mesmo_tempo_nao_quebram_o_aleatorio()
    {
        var sorteador = new SorteadorDeCriaturas(new Random(9));

        int ContarElites()
        {
            var elites = 0;
            for (var i = 0; i < 5000; i++)
            {
                if (sorteador.Sortear(Javali).Variante == VarianteDeCriatura.Elite)
                {
                    elites++;
                }
            }

            return elites;
        }

        var tarefas = Enumerable.Range(0, 8).Select(_ => Task.Run(ContarElites));
        var total = (await Task.WhenAll(tarefas)).Sum();

        // Um Random usado sem trava por várias threads degenera e passa a devolver só zero:
        // todo sorteio viraria elite.
        Assert.InRange(total / 40000.0, 0.04, 0.06);
    }
}
