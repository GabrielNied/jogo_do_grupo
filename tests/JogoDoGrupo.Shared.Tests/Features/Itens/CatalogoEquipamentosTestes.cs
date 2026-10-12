using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class CatalogoEquipamentosTestes
{
    [Fact]
    public void CatalogoContemQuatroFamiliasDuasVariantesENoveCategoriasSemRepeticao()
    {
        var modelos = Enumerable.Range(101, 72).Select(CatalogoEquipamentos.Obter).ToArray();
        Assert.All(modelos, Assert.NotNull);
        Assert.Equal(72, modelos.Select(m => m.Nome).Distinct().Count());
        Assert.Equal(4, modelos.Select(m => m.Familia).Distinct().Count());
        foreach (var familia in modelos.GroupBy(m => m.Familia))
        {
            Assert.Equal(18, familia.Count());
            foreach (var variante in familia.GroupBy(m => m.Variacao))
                Assert.Equal(Enumerable.Range(0, 9), variante.Select(m => (int)m.Tipo));
        }
        Assert.All(
            modelos,
            m =>
            {
                Assert.All(m.Atributos, a => Assert.True(a.Fixo));
                Assert.Equal(
                    m.Atributos.Length,
                    m.Atributos.Select(a => a.Status).Distinct().Count()
                );
            }
        );
    }

    [Theory]
    [InlineData(101, "Elmo do Vigia", StatusItem.Vitalidade, 6, 16)]
    [InlineData(111, "Peitoral do Bastião", StatusItem.Vitalidade, 4, 38)]
    [InlineData(126, "Brinco do Predador", StatusItem.Forca, 6, 0)]
    [InlineData(133, "Cinto do Ruptor", StatusItem.Forca, 4, 5)]
    [InlineData(140, "Calças da Aurora", StatusItem.Intelecto, 6, 6)]
    [InlineData(150, "Botas da Tempestade", StatusItem.Intelecto, 4, 3)]
    [InlineData(157, "Braçadeiras do Batedor", StatusItem.Destreza, 6, 4)]
    [InlineData(172, "Talismã do Olho Longo", StatusItem.Destreza, 4, 0)]
    public void ValoresDoLivro(
        int id,
        string nome,
        StatusItem principal,
        double valor,
        double defesa
    )
    {
        var item = CatalogoEquipamentos.Obter(id);
        Assert.Equal(nome, item.Nome);
        Assert.Equal(valor, item.Atributos.Single(a => a.Status == principal).Valor);
        Assert.Equal(
            defesa,
            item.Atributos.Where(a => a.Status == StatusItem.Defesa).Sum(a => a.Valor)
        );
    }

    [Theory]
    [InlineData(128, StatusItem.Vigor, 10)]
    [InlineData(146, StatusItem.Espirito, 3)]
    [InlineData(164, StatusItem.Vigor, 10)]
    public void VariantesBTemEspecializacao(int id, StatusItem status, double valor) =>
        Assert.Equal(
            valor,
            CatalogoEquipamentos.Obter(id).Atributos.Single(a => a.Status == status).Valor
        );

    [Fact]
    public void ExemplaresIndependentesEIdsLegadosPreservados()
    {
        var item = CatalogoEquipamentos.Obter(101);
        item.Atributos[0].Valor = 999;
        Assert.Equal(6, CatalogoEquipamentos.Obter(101).Atributos[0].Valor);
        Assert.Equal("Insígnia do Vigia", CatalogoEquipamentos.Obter(109).Nome);
        Assert.True(CatalogoEquipamentos.Obter(100).DuasMaos);
        Assert.NotNull(CatalogoEquipamentos.Obter(1));
        Assert.NotNull(CatalogoEquipamentos.Obter(2));
        Assert.Null(CatalogoEquipamentos.Obter(3));
        Assert.Null(CatalogoEquipamentos.Obter(173));
        Assert.Null(CatalogoEquipamentos.Obter(-1));
        Assert.Equal(Enumerable.Range(100, 73), CatalogoEquipamentos.ModelosDeTeste);
    }
}
