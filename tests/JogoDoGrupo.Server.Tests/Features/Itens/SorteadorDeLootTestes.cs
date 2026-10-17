using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class SorteadorDeLootTestes
{
    private static List<QuedaDeLoot[]> Sortear(
        int vezes,
        int semente,
        OrigemDoLoot origem = OrigemDoLoot.Comum,
        int nivel = 3
    )
    {
        var sorteador = new SorteadorDeLoot(new Random(semente));
        return Enumerable.Range(0, vezes).Select(_ => sorteador.Sortear(origem, nivel)).ToList();
    }

    private static bool EhPecaDoCatalogo(QuedaDeLoot q) =>
        q.ModeloId >= CatalogoEquipamentos.PrimeiroModelo
        && q.ModeloId
            < CatalogoEquipamentos.PrimeiroModelo + CatalogoEquipamentos.QuantidadeModelos;

    [Fact]
    public void Cada_sorteio_da_de_zero_ao_maximo_de_pecas_do_catalogo()
    {
        var sorteios = Sortear(2000, semente: 1);

        Assert.All(
            sorteios,
            quedas =>
            {
                Assert.InRange(quedas.Length, 0, RegrasLoot.MaximoPorInimigo);
                Assert.All(quedas, q => Assert.True(EhPecaDoCatalogo(q), $"modelo {q.ModeloId}"));
            }
        );
    }

    [Fact]
    public void Itens_de_teste_mortos_nao_caem_e_aparecem_as_72_pecas()
    {
        var modelos = Sortear(20000, semente: 2)
            .SelectMany(q => q)
            .Select(q => q.ModeloId)
            .ToHashSet();

        Assert.DoesNotContain(CatalogoItens.PocaoDeTeste, modelos);
        Assert.DoesNotContain(CatalogoItens.MoedaDeTeste, modelos);
        Assert.Equal(CatalogoEquipamentos.QuantidadeModelos, modelos.Count);
    }

    [Fact]
    public void As_quantidades_seguem_as_chances_das_regras()
    {
        const int vezes = 20000;
        var sorteios = Sortear(vezes, semente: 3);

        double Fracao(int quantidade) =>
            sorteios.Count(q => q.Length == quantidade) / (double)vezes;

        Assert.Equal(RegrasLoot.ChanceDeNenhumItem, Fracao(0), 0.02);
        Assert.Equal(RegrasLoot.ChanceDeUmItem, Fracao(1), 0.02);
        Assert.Equal(RegrasLoot.ChanceDeDoisItens, Fracao(2), 0.02);
    }

    [Theory]
    [InlineData(1, GrauEquipamento.Inicial)]
    [InlineData(9, GrauEquipamento.Inicial)]
    [InlineData(10, GrauEquipamento.Medio)]
    [InlineData(19, GrauEquipamento.Medio)]
    [InlineData(20, GrauEquipamento.Forte)]
    [InlineData(30, GrauEquipamento.Forte)]
    public void O_nivel_do_inimigo_define_o_grau_de_todas_as_pecas(int nivel, GrauEquipamento grau)
    {
        var quedas = Sortear(500, semente: 4, nivel: nivel).SelectMany(q => q).ToList();

        Assert.NotEmpty(quedas);
        Assert.All(quedas, q => Assert.Equal(grau, q.Grau));
    }

    [Theory]
    [InlineData(OrigemDoLoot.Comum)]
    [InlineData(OrigemDoLoot.Elite)]
    [InlineData(OrigemDoLoot.ChefeRaro)]
    [InlineData(OrigemDoLoot.Chefe)]
    public void A_queda_guarda_a_origem_do_inimigo(OrigemDoLoot origem)
    {
        var quedas = Sortear(500, semente: 5, origem).SelectMany(q => q).ToList();

        Assert.NotEmpty(quedas);
        Assert.All(quedas, q => Assert.Equal(origem, q.Origem));
    }

    [Fact]
    public void A_mesma_semente_da_o_mesmo_resultado()
    {
        static string Resumo(List<QuedaDeLoot[]> s) =>
            string.Join(
                ";",
                s.Select(q => string.Join(",", q.Select(x => $"{x.ModeloId}/{x.Origem}/{x.Grau}")))
            );

        Assert.Equal(Resumo(Sortear(50, semente: 7)), Resumo(Sortear(50, semente: 7)));
    }

    [Fact]
    public void Cada_chamada_e_um_sorteio_individual_e_novo()
    {
        // Dois participantes do mesmo inimigo não recebem o mesmo resultado em bloco.
        var sorteador = new SorteadorDeLoot(new Random(8));
        var pares = Enumerable
            .Range(0, 500)
            .Select(_ =>
                (sorteador.Sortear(OrigemDoLoot.Comum, 3), sorteador.Sortear(OrigemDoLoot.Comum, 3))
            )
            .ToList();

        Assert.Contains(
            pares,
            p => !p.Item1.Select(q => q.ModeloId).SequenceEqual(p.Item2.Select(q => q.ModeloId))
        );
    }

    [Fact]
    public void Sorteios_simultaneos_com_gerador_proprio_nao_quebram()
    {
        var sorteador = new SorteadorDeLoot(new Random(4));

        Parallel.For(
            0,
            2000,
            _ =>
                Assert.InRange(
                    sorteador.Sortear(OrigemDoLoot.Elite, 12).Length,
                    0,
                    RegrasLoot.MaximoPorInimigo
                )
        );
    }

    [Theory]
    [InlineData(OrigemDoLoot.Chefe)]
    [InlineData(OrigemDoLoot.ChefeRaro)]
    public void Chefes_podem_dropar_item_raro_de_montaria(OrigemDoLoot origem)
    {
        var sorteios = Sortear(1000, semente: 42, origem: origem);
        var modelos = sorteios.SelectMany(q => q).Select(q => q.ModeloId).ToHashSet();

        Assert.Contains(TabelaDeLoot.ItemMontariaChefe, modelos);
    }

    [Theory]
    [InlineData(OrigemDoLoot.Comum)]
    [InlineData(OrigemDoLoot.Elite)]
    public void Comum_e_elite_nunca_dropam_montaria(OrigemDoLoot origem)
    {
        var sorteios = Sortear(1000, semente: 42, origem: origem);
        var modelos = sorteios.SelectMany(q => q).Select(q => q.ModeloId).ToHashSet();

        Assert.DoesNotContain(TabelaDeLoot.ItemMontariaChefe, modelos);
    }
}
