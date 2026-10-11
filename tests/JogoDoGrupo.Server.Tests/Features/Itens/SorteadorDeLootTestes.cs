using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class SorteadorDeLootTestes
{
    private static List<int[]> Sortear(int vezes, int semente)
    {
        var sorteador = new SorteadorDeLoot(new Random(semente));
        return Enumerable.Range(0, vezes).Select(_ => sorteador.Sortear()).ToList();
    }

    [Fact]
    public void Cada_sorteio_da_de_zero_ao_maximo_de_itens_do_catalogo()
    {
        var sorteios = Sortear(2000, semente: 1);

        Assert.All(
            sorteios,
            itens =>
            {
                Assert.InRange(itens.Length, 0, RegrasLoot.MaximoPorInimigo);
                Assert.All(itens, id => Assert.Contains(id, CatalogoItens.Todos));
            }
        );
    }

    [Fact]
    public void Aparecem_todas_as_quantidades_e_todos_os_itens()
    {
        var sorteios = Sortear(2000, semente: 2);

        Assert.Equal(
            new[] { 0, 1, 2 },
            sorteios.Select(itens => itens.Length).Distinct().Order().ToArray()
        );
        Assert.Equal(
            CatalogoItens.Todos,
            sorteios.SelectMany(itens => itens).Distinct().Order().ToArray()
        );
    }

    [Fact]
    public void As_quantidades_seguem_as_chances_das_regras()
    {
        const int vezes = 20000;
        var sorteios = Sortear(vezes, semente: 3);

        double Fracao(int quantidade) =>
            sorteios.Count(itens => itens.Length == quantidade) / (double)vezes;

        Assert.Equal(RegrasLoot.ChanceDeNenhumItem, Fracao(0), 0.02);
        Assert.Equal(RegrasLoot.ChanceDeUmItem, Fracao(1), 0.02);
        Assert.Equal(RegrasLoot.ChanceDeDoisItens, Fracao(2), 0.02);
    }

    [Fact]
    public void A_mesma_semente_da_o_mesmo_resultado()
    {
        Assert.Equal(Sortear(50, semente: 7), Sortear(50, semente: 7));
    }

    [Fact]
    public void Sorteios_simultaneos_com_gerador_proprio_nao_quebram()
    {
        var sorteador = new SorteadorDeLoot(new Random(4));

        Parallel.For(
            0,
            2000,
            _ => Assert.InRange(sorteador.Sortear().Length, 0, RegrasLoot.MaximoPorInimigo)
        );
    }
}
