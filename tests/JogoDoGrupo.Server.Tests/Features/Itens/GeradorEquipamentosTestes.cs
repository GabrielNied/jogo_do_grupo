using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class GeradorEquipamentosTestes
{
    [Theory]
    [InlineData(GrauEquipamento.Inicial, 1, 1)]
    [InlineData(GrauEquipamento.Medio, 2, 10)]
    [InlineData(GrauEquipamento.Forte, 4, 20)]
    public void TodosOsModelosAplicamGrauUmaVezSemAlterarBase(
        GrauEquipamento grau,
        int fator,
        int requisito
    )
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        foreach (var id in Enumerable.Range(101, 72))
        {
            var inicial = gerador.Gerar(id, new Random(7))!;
            var item = gerador.Gerar(id, new Random(7), RaridadeItem.Lendario, grau)!;
            Assert.Equal(
                inicial.Atributos.Select(a => a.Valor * fator),
                item.Atributos.Select(a => a.Valor)
            );
            Assert.Equal(
                inicial.Atributos.Select(a => a.Status),
                item.Atributos.Select(a => a.Status)
            );
            Assert.Equal(1, item.Nivel);
            Assert.Equal(requisito, item.NivelPersonagemMinimo);
            Assert.Equal(RaridadeItem.Lendario, item.Raridade);
            Assert.Equal(inicial.Familia, item.Familia);
            Assert.Equal(inicial.Variacao, item.Variacao);
            Assert.Equal(
                inicial.Atributos.Where(a => a.Fixo).Select(a => a.Valor),
                CatalogoEquipamentos.Obter(id).Atributos.Select(a => a.Valor)
            );
        }
    }

    [Fact]
    public void Gerar_NaoRepeteStatusEPreservaFixoDoModelo()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        var item = gerador.Gerar(100, new Random(7))!;
        Assert.Equal(20, item.Atributos.Single(a => a.Fixo).Valor);
        Assert.Equal(3, item.Atributos.Length);
        Assert.Equal(3, item.Atributos.Select(a => a.Status).Distinct().Count());
        Assert.InRange(item.IndiceInfusivel, 0, 2);
        var igual = gerador.Gerar(100, new Random(7))!;
        Assert.Equal(item.Atributos.Select(a => a.Status), igual.Atributos.Select(a => a.Status));
    }

    [Fact]
    public void GrauNaoMudaRaridadeNemNivelDaPeca()
    {
        var item = new GeradorEquipamentos(new ConfigEquipamentos()).Gerar(
            100,
            new Random(7),
            RaridadeItem.Epico,
            GrauEquipamento.Forte
        )!;
        Assert.Equal(80, item.Atributos.Single(a => a.Fixo).Valor);
        Assert.Equal(RaridadeItem.Epico, item.Raridade);
        Assert.Equal(1, item.Nivel);
        Assert.Equal(20, item.NivelPersonagemMinimo);
    }

    [Fact]
    public void PoolInvalidoRecusaSemSortearENaoEquipamentoNaoGera()
    {
        var gerador = new GeradorEquipamentos(
            new ConfigEquipamentos { QuantidadeAleatorios = 100 }
        );
        Assert.Throws<InvalidOperationException>(() => gerador.Gerar(100, new Random(7)));
        Assert.Null(gerador.Gerar(CatalogoItens.PocaoDeTeste, new Random(7)));
    }
}
