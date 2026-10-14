using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class GeradorEquipamentosTestes
{
    private static readonly int[] Modelos = Enumerable.Range(100, 73).ToArray();

    [Theory]
    [InlineData(GrauEquipamento.Inicial, 0, 60, 1)]
    [InlineData(GrauEquipamento.Medio, 20, 80, 10)]
    [InlineData(GrauEquipamento.Forte, 40, 100, 20)]
    public void Todos_os_modelos_sorteiam_inteiros_na_janela_do_grau(
        GrauEquipamento grau,
        int minimo,
        int maximo,
        int requisito
    )
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        foreach (var id in Modelos)
        {
            for (var semente = 0; semente < 20; semente++)
            {
                var item = gerador.Gerar(id, new Random(semente), OrigemDoLoot.Comum, grau)!;
                Assert.All(
                    item.Atributos,
                    a =>
                    {
                        Assert.Equal(Math.Round(a.Valor), a.Valor);
                        Assert.InRange(a.Valor, minimo, maximo);
                    }
                );
                Assert.Equal(grau, item.Grau);
                Assert.Equal(1, item.Nivel);
                Assert.Equal(requisito, item.NivelPersonagemMinimo);
            }
        }
    }

    [Fact]
    public void Grava_o_tier_de_origem_calculado_pelos_valores_e_a_versao_da_escala()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        foreach (var id in Modelos)
        {
            var item = gerador.Gerar(id, new Random(id), OrigemDoLoot.Fortalecida)!;

            Assert.Equal(RegrasTier.VersaoAtual, item.VersaoDosAtributos);
            Assert.Equal(RegrasTier.TierDe(item), item.TierOrigem);
            // Raridade é só legado: o gerador não a sorteia mais.
            Assert.Equal(RaridadeItem.Comum, item.Raridade);
        }
    }

    [Fact]
    public void Origem_melhor_da_valores_e_tiers_melhores()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());

        (double Valor, double Tier) Media(OrigemDoLoot origem)
        {
            var aleatorio = new Random(21);
            var itens = Enumerable
                .Range(0, 4000)
                .Select(i =>
                    gerador.Gerar(
                        CatalogoEquipamentos.PrimeiroModelo
                            + i % CatalogoEquipamentos.QuantidadeModelos,
                        aleatorio,
                        origem,
                        GrauEquipamento.Medio
                    )!
                )
                .ToList();
            return (
                itens.Average(i => i.Atributos.Average(a => a.Valor)),
                itens.Average(i => (int)i.TierOrigem)
            );
        }

        var comum = Media(OrigemDoLoot.Comum);
        var fortalecida = Media(OrigemDoLoot.Fortalecida);
        var chefeRaro = Media(OrigemDoLoot.ChefeRaro);
        var chefe = Media(OrigemDoLoot.Chefe);

        Assert.True(comum.Valor < fortalecida.Valor, $"{comum} {fortalecida}");
        Assert.True(fortalecida.Valor < chefeRaro.Valor, $"{fortalecida} {chefeRaro}");
        Assert.True(chefeRaro.Valor < chefe.Valor, $"{chefeRaro} {chefe}");
        Assert.True(comum.Tier < chefe.Tier, $"{comum} {chefe}");
    }

    [Fact]
    public void Origem_so_melhora_os_valores_o_resto_do_exemplar_fica_igual()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        var comum = gerador.Gerar(102, new Random(7))!;
        var chefe = gerador.Gerar(102, new Random(7), OrigemDoLoot.Chefe)!;

        Assert.Equal(comum.Atributos.Select(a => a.Status), chefe.Atributos.Select(a => a.Status));
        Assert.Equal(comum.Familia, chefe.Familia);
        Assert.Equal(comum.Raridade, chefe.Raridade);
    }

    [Fact]
    public void Gerar_NaoRepeteStatusEPreservaOsAtributosDoModelo()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        var item = gerador.Gerar(100, new Random(7))!;
        var modelo = CatalogoEquipamentos.Obter(100)!.Atributos;
        Assert.Equal(
            modelo.Select(a => a.Status),
            item.Atributos.Take(modelo.Length).Select(a => a.Status)
        );
        Assert.All(item.Atributos.Take(modelo.Length), a => Assert.True(a.Fixo));
        Assert.Contains(item.Atributos, a => a.Fixo && a.Status == StatusItem.DanoFisico);
        Assert.Equal(
            item.Atributos.Length,
            item.Atributos.Select(a => a.Status).Distinct().Count()
        );
        Assert.Equal(0, item.NivelFusao);
        var igual = gerador.Gerar(100, new Random(7))!;
        Assert.Equal(item.Atributos.Select(a => a.Status), igual.Atributos.Select(a => a.Status));
        Assert.Equal(item.Atributos.Select(a => a.Valor), igual.Atributos.Select(a => a.Valor));
    }

    [Fact]
    public void Extras_so_vem_de_atributos_elegiveis_na_categoria()
    {
        // Capacete: elegíveis são os da ConfigTier; PotenciaFogo não tem faixa e nunca sai.
        var gerador = new GeradorEquipamentos(
            new ConfigEquipamentos
            {
                StatusAleatorios =
                [
                    StatusItem.PotenciaFogo,
                    StatusItem.DanoFisico,
                    StatusItem.RegeneracaoMana,
                ],
                QuantidadeAleatorios = 2,
            }
        );
        for (var semente = 0; semente < 30; semente++)
        {
            var item = gerador.Gerar(101, new Random(semente))!;
            Assert.DoesNotContain(item.Atributos, a => a.Status == StatusItem.PotenciaFogo);
        }

        // Sem candidato elegível na categoria, o item sai só com os atributos do modelo.
        var semCandidatos = new GeradorEquipamentos(
            new ConfigEquipamentos
            {
                StatusAleatorios = [StatusItem.PotenciaFogo],
                QuantidadeAleatorios = 1,
            }
        ).Gerar(101, new Random(1))!;
        Assert.DoesNotContain(semCandidatos.Atributos, a => a.Status == StatusItem.PotenciaFogo);
    }

    [Fact]
    public void PoolInvalidoRecusaSemSortearENaoEquipamentoNaoGera()
    {
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos { QuantidadeAleatorios = -1 });
        Assert.Throws<InvalidOperationException>(() => gerador.Gerar(100, new Random(7)));
        Assert.Null(gerador.Gerar(CatalogoItens.PocaoDeTeste, new Random(7)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeradorEquipamentos(new ConfigEquipamentos()).Gerar(
                100,
                new Random(7),
                (OrigemDoLoot)9
            )
        );
    }
}
