using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class TabelaDeLootTestes
{
    [Theory]
    [InlineData(CategoriaDeCriatura.Comum, VarianteDeCriatura.Normal, OrigemDoLoot.Comum)]
    [InlineData(
        CategoriaDeCriatura.Comum,
        VarianteDeCriatura.Fortalecida,
        OrigemDoLoot.Fortalecida
    )]
    [InlineData(CategoriaDeCriatura.ChefeRaro, VarianteDeCriatura.Normal, OrigemDoLoot.ChefeRaro)]
    [InlineData(CategoriaDeCriatura.Chefe, VarianteDeCriatura.Normal, OrigemDoLoot.Chefe)]
    public void Origem_vem_da_categoria_e_da_variante(
        CategoriaDeCriatura categoria,
        VarianteDeCriatura variante,
        OrigemDoLoot esperada
    ) => Assert.Equal(esperada, TabelaDeLoot.OrigemDe(categoria, variante));

    [Theory]
    [InlineData(-5, GrauEquipamento.Inicial, 1)]
    [InlineData(0, GrauEquipamento.Inicial, 1)]
    [InlineData(1, GrauEquipamento.Inicial, 1)]
    [InlineData(9, GrauEquipamento.Inicial, 1)]
    [InlineData(10, GrauEquipamento.Medio, 10)]
    [InlineData(19, GrauEquipamento.Medio, 10)]
    [InlineData(20, GrauEquipamento.Forte, 20)]
    [InlineData(99, GrauEquipamento.Forte, 20)]
    public void Nivel_do_inimigo_define_grau_e_requisito(
        int nivel,
        GrauEquipamento grau,
        int requisito
    )
    {
        Assert.Equal(grau, TabelaDeLoot.GrauDoNivel(nivel));
        Assert.Equal(requisito, TabelaDeLoot.NivelMinimoDoGrau(grau));
    }

    [Theory]
    [InlineData(OrigemDoLoot.Comum)]
    [InlineData(OrigemDoLoot.Fortalecida)]
    [InlineData(OrigemDoLoot.ChefeRaro)]
    [InlineData(OrigemDoLoot.Chefe)]
    public void Pesos_tem_uma_entrada_por_raridade_e_algum_peso(OrigemDoLoot origem)
    {
        var pesos = TabelaDeLoot.PesosDe(origem);

        Assert.Equal(Enum.GetValues<RaridadeItem>().Length, pesos.Length);
        Assert.All(pesos, p => Assert.True(p >= 0));
        Assert.True(pesos.Sum() > 0);
    }

    [Fact]
    public void Raridade_sorteada_respeita_as_pontas_e_nunca_cai_em_peso_zero()
    {
        Assert.Equal(RaridadeItem.Comum, TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Comum, 0));
        Assert.Equal(RaridadeItem.Comum, TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Comum, -3));
        // O chefe tem peso zero em Comum: o começo da faixa já é Incomum.
        Assert.Equal(RaridadeItem.Incomum, TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Chefe, 0));
        Assert.Equal(RaridadeItem.Lendario, TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Chefe, 7));
        Assert.Equal(
            RaridadeItem.Lendario,
            TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Chefe, 0.99999)
        );
        // O comum sem variante tem peso zero em Lendário: nunca sai.
        Assert.Equal(
            RaridadeItem.Epico,
            TabelaDeLoot.RaridadeSorteada(OrigemDoLoot.Comum, 0.9999999)
        );
    }

    [Fact]
    public void Origens_mais_valiosas_deslocam_o_sorteio_para_raridades_maiores()
    {
        for (var sorteio = 0.05; sorteio < 1; sorteio += 0.05)
        {
            var anterior = RaridadeItem.Comum;
            foreach (
                var origem in new[]
                {
                    OrigemDoLoot.Comum,
                    OrigemDoLoot.Fortalecida,
                    OrigemDoLoot.ChefeRaro,
                    OrigemDoLoot.Chefe,
                }
            )
            {
                var atual = TabelaDeLoot.RaridadeSorteada(origem, sorteio);
                Assert.True(
                    atual >= anterior,
                    $"sorteio {sorteio}: {origem} deu {atual} < {anterior}"
                );
                anterior = atual;
            }
        }
    }

    [Fact]
    public void Pontuacao_soma_atributos_e_cresce_com_a_raridade()
    {
        var peca = CatalogoEquipamentos.Obter(CatalogoEquipamentos.PrimeiroModelo);
        var soma = peca.Atributos.Sum(a => a.Valor);

        Assert.Equal(soma, TabelaDeLoot.Pontuacao(peca), 6);
        peca.Raridade = RaridadeItem.Raro;
        Assert.Equal(
            soma * (1 + 2 * ConfigLoot.BonusDePontuacaoPorRaridade),
            TabelaDeLoot.Pontuacao(peca),
            6
        );
        Assert.Equal(0, TabelaDeLoot.Pontuacao(null!));
    }

    [Fact]
    public void Nomes_de_raridade_cobrem_as_cinco()
    {
        Assert.Equal(
            new[] { "Comum", "Incomum", "Raro", "Épico", "Lendário" },
            Enum.GetValues<RaridadeItem>().Select(RegrasEquipamentos.NomeDaRaridade)
        );
    }
}
