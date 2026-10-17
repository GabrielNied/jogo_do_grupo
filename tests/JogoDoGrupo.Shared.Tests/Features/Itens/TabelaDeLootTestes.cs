using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class TabelaDeLootTestes
{
    [Theory]
    [InlineData(CategoriaDeCriatura.Comum, VarianteDeCriatura.Normal, OrigemDoLoot.Comum)]
    [InlineData(CategoriaDeCriatura.Comum, VarianteDeCriatura.Elite, OrigemDoLoot.Elite)]
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

    [Fact]
    public void Origens_mais_valiosas_ganham_mais_rolagens()
    {
        var rolagens = new[]
        {
            OrigemDoLoot.Comum,
            OrigemDoLoot.Elite,
            OrigemDoLoot.ChefeRaro,
            OrigemDoLoot.Chefe,
        }
            .Select(TabelaDeLoot.RolagensDe)
            .ToArray();

        Assert.Equal(1, rolagens[0]);
        Assert.Equal(rolagens.OrderBy(r => r), rolagens);
        Assert.Equal(rolagens.Length, rolagens.Distinct().Count());
    }

    [Fact]
    public void Sortear_guarda_origem_e_grau_e_gasta_um_numero()
    {
        var aleatorio = new Random(5);
        var queda = TabelaDeLoot.Sortear(aleatorio, OrigemDoLoot.Comum, 12);

        Assert.Equal(OrigemDoLoot.Comum, queda.Origem);
        Assert.Equal(GrauEquipamento.Medio, queda.Grau);
        Assert.InRange(
            queda.ModeloId,
            CatalogoEquipamentos.PrimeiroModelo,
            CatalogoEquipamentos.PrimeiroModelo + CatalogoEquipamentos.QuantidadeModelos - 1
        );
        var referencia = new Random(5);
        referencia.Next(CatalogoEquipamentos.QuantidadeModelos);
        Assert.Equal(referencia.Next(), aleatorio.Next());
    }

    [Fact]
    public void Chefe_raro_pode_largar_item_exclusivo()
    {
        var modelos = new HashSet<int>();
        var aleatorio = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            var queda = TabelaDeLoot.Sortear(aleatorio, OrigemDoLoot.ChefeRaro, 10);
            modelos.Add(queda.ModeloId);
        }

        Assert.Contains(TabelaDeLoot.ModeloExclusivoDoChefeRaro, modelos);
        Assert.True(modelos.Count > 1);
    }

    [Theory]
    [InlineData(OrigemDoLoot.Comum)]
    [InlineData(OrigemDoLoot.Elite)]
    [InlineData(OrigemDoLoot.Chefe)]
    public void Outras_origens_nunca_largam_o_item_exclusivo_do_chefe_raro(OrigemDoLoot origem)
    {
        var aleatorio = new Random(123);
        for (var i = 0; i < 200; i++)
        {
            var queda = TabelaDeLoot.Sortear(aleatorio, origem, 10);
            Assert.NotEqual(TabelaDeLoot.ModeloExclusivoDoChefeRaro, queda.ModeloId);
            Assert.InRange(
                queda.ModeloId,
                CatalogoEquipamentos.PrimeiroModelo,
                CatalogoEquipamentos.PrimeiroModelo + CatalogoEquipamentos.QuantidadeModelos - 1
            );
        }
    }

    [Fact]
    public void Pontuacao_soma_valores_e_cresce_com_o_tier()
    {
        var peca = new Equipamento
        {
            Tipo = TipoEquipamento.Peito,
            Atributos = new[]
            {
                new AtributoItem { Status = StatusItem.DanoFisico, Valor = 90 },
                new AtributoItem { Status = StatusItem.VidaMaxima, Valor = 70 },
            },
        };
        Assert.Equal(TierItem.S, RegrasTier.TierDe(peca));

        Assert.Equal(
            160 * (1 + (int)TierItem.S * ConfigLoot.BonusDePontuacaoPorTier),
            TabelaDeLoot.Pontuacao(peca),
            6
        );
        peca.Raridade = RaridadeItem.Lendario;
        Assert.Equal(
            160 * (1 + (int)TierItem.S * ConfigLoot.BonusDePontuacaoPorTier),
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
