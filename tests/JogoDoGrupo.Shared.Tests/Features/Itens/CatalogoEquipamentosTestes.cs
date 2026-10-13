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

    [Fact]
    public void Todo_atributo_do_modelo_e_elegivel_na_categoria_e_o_modelo_tem_algum()
    {
        foreach (var id in CatalogoEquipamentos.ModelosDeTeste)
        {
            var modelo = CatalogoEquipamentos.Obter(id);
            Assert.NotEmpty(modelo.Atributos);
            Assert.All(
                modelo.Atributos,
                a =>
                    Assert.True(
                        ConfigAtributosItem.Elegivel(modelo.Tipo, a.Status),
                        $"{id} {a.Status}"
                    )
            );
        }
    }

    [Theory]
    // Principal da família (Vigor, Força, Intelecto, Agilidade) mais o atributo da categoria, só
    // o que a categoria aceita (a matriz de itens.md).
    [InlineData(101, "Elmo do Vigia", new[] { StatusItem.Vigor, StatusItem.DefesaFisica })]
    [InlineData(
        111,
        "Peitoral do Bastião",
        new[] { StatusItem.Vigor, StatusItem.DefesaFisica, StatusItem.DefesaMagica }
    )]
    [InlineData(126, "Brinco do Predador", new[] { StatusItem.Forca, StatusItem.DanoCritico })]
    // Força não existe no cinto: fica o Vigor da categoria e a Agilidade da variante B.
    [InlineData(133, "Cinto do Ruptor", new[] { StatusItem.Vigor, StatusItem.Agilidade })]
    [InlineData(140, "Calças da Aurora", new[] { StatusItem.Intelecto, StatusItem.DefesaFisica })]
    // Espírito (Tempestade B) não existe em botas.
    [InlineData(
        150,
        "Botas da Tempestade",
        new[] { StatusItem.Intelecto, StatusItem.DefesaFisica }
    )]
    [InlineData(
        157,
        "Braçadeiras do Batedor",
        new[] { StatusItem.Agilidade, StatusItem.DefesaFisica }
    )]
    // A especialização do Olho Longo B é a Agilidade, que já é o principal: não repete.
    [InlineData(
        172,
        "Talismã do Olho Longo",
        new[] { StatusItem.Agilidade, StatusItem.DefesaMagica }
    )]
    public void AtributosDoModelo(int id, string nome, StatusItem[] esperados)
    {
        var item = CatalogoEquipamentos.Obter(id);
        Assert.Equal(nome, item.Nome);
        Assert.Equal(esperados, item.Atributos.Select(a => a.Status));
    }

    [Theory]
    [InlineData(128, StatusItem.Agilidade)] // Ruptor B (capacete)
    [InlineData(132, StatusItem.Agilidade)] // Ruptor B (botas)
    [InlineData(151, StatusItem.Espirito)] // Tempestade B (cinto)
    [InlineData(110, StatusItem.DefesaMagica)] // Bastião B (capacete)
    public void VariantesBTemEspecializacao(int id, StatusItem status) =>
        Assert.Contains(CatalogoEquipamentos.Obter(id).Atributos, a => a.Status == status);

    [Theory]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma, TipoEquipamento.Espada)]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma + 1, TipoEquipamento.Arco)]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma + 2, TipoEquipamento.Cajado)]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma + 3, TipoEquipamento.Escudo)]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma + 4, TipoEquipamento.Foco)]
    public void ArmasDeTesteTemODanoDaArmaEOPrincipalDaCategoria(int id, TipoEquipamento tipo)
    {
        var item = CatalogoEquipamentos.Obter(id);
        Assert.Equal(tipo, item.Tipo);
        var status = item.Atributos.Select(a => a.Status).ToArray();
        Assert.Equal(ConfigAtributosItem.EhArma(tipo), status.Contains(StatusItem.DanoDaArma));
        Assert.Equal(
            tipo == TipoEquipamento.Cajado || tipo == TipoEquipamento.Foco,
            status.Contains(StatusItem.Intelecto)
        );
    }

    [Fact]
    public void ExemplaresIndependentesEIdsLegadosPreservados()
    {
        var item = CatalogoEquipamentos.Obter(101);
        item.Atributos[0].Valor = 999;
        Assert.Equal(0, CatalogoEquipamentos.Obter(101).Atributos[0].Valor);
        Assert.Equal("Insígnia do Vigia", CatalogoEquipamentos.Obter(109).Nome);
        Assert.True(CatalogoEquipamentos.Obter(100).DuasMaos);
        Assert.NotNull(CatalogoEquipamentos.Obter(1));
        Assert.NotNull(CatalogoEquipamentos.Obter(2));
        Assert.Null(CatalogoEquipamentos.Obter(3));
        Assert.NotNull(CatalogoEquipamentos.Obter(173));
        Assert.Null(CatalogoEquipamentos.Obter(178));
        Assert.Null(CatalogoEquipamentos.Obter(-1));
        Assert.Equal(Enumerable.Range(100, 78), CatalogoEquipamentos.ModelosDeTeste);
    }
}
