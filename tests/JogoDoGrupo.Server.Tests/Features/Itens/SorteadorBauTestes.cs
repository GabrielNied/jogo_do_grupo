using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class SorteadorBauTestes
{
    private static List<int[]> Sortear(int vezes, int semente)
    {
        var sorteador = new SorteadorBau(new Random(semente));
        return Enumerable.Range(0, vezes).Select(_ => sorteador.Sortear()).ToList();
    }

    [Fact]
    public void Cada_abertura_da_de_um_a_tres_itens_do_catalogo()
    {
        var aberturas = Sortear(2000, semente: 1);

        Assert.All(
            aberturas,
            itens =>
            {
                Assert.InRange(
                    itens.Length,
                    RegrasItens.MinimoItensPorBau,
                    RegrasItens.MaximoItensPorBau
                );
                Assert.All(itens, id => Assert.Contains(id, CatalogoItens.Todos));
            }
        );
    }

    [Fact]
    public void Aparecem_todas_as_quantidades_e_todos_os_itens()
    {
        var aberturas = Sortear(2000, semente: 2);

        Assert.Equal(
            new[] { 1, 2, 3 },
            aberturas.Select(itens => itens.Length).Distinct().Order().ToArray()
        );
        Assert.Equal(
            CatalogoItens.Todos,
            aberturas.SelectMany(itens => itens).Distinct().Order().ToArray()
        );
    }

    [Fact]
    public void Itens_sao_aproximadamente_uniformes_e_podem_repetir_na_mesma_abertura()
    {
        var aberturas = Sortear(4000, semente: 3);
        var todos = aberturas.SelectMany(itens => itens).ToList();

        foreach (var id in CatalogoItens.Todos)
        {
            var fracao = todos.Count(x => x == id) / (double)todos.Count;
            Assert.InRange(fracao, 0.22, 0.28);
        }

        Assert.Contains(aberturas, itens => itens.Distinct().Count() < itens.Length);
    }

    [Fact]
    public void Mesma_semente_da_o_mesmo_resultado()
    {
        Assert.Equal(Sortear(50, semente: 7), Sortear(50, semente: 7));
    }
}
