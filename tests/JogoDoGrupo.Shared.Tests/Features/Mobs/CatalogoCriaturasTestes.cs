using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class CatalogoCriaturasTestes
{
    [Fact]
    public void Ids_sao_os_combinados_unicos_e_em_ordem()
    {
        Assert.Equal(1, CatalogoCriaturas.JavaliDoCampo);
        Assert.Equal(100, CatalogoCriaturas.GuardiaoDoReflexo);
        Assert.Equal(200, CatalogoCriaturas.PresaVelha);
        Assert.Equal(new[] { 1, 100, 200 }, CatalogoCriaturas.Todas.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Javali_do_campo_tem_os_numeros_da_proposta()
    {
        var javali = CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliDoCampo);

        Assert.Equal("Javali do Campo", javali.Nome);
        Assert.Equal(CategoriaDeCriatura.Comum, javali.Categoria);
        Assert.Equal(3, javali.Nivel);
        Assert.Equal(180, javali.VidaBase);
        Assert.Equal(40, javali.DanoBase);
        Assert.Equal(20, javali.ExperienciaBase);
        Assert.Equal(1.0, javali.ChanceDeAparecer);
        Assert.Equal(120, javali.IntervaloDeAparicaoSegundos);
    }

    [Fact]
    public void Chefe_sempre_aparece_e_chefe_raro_nem_sempre()
    {
        var chefe = CatalogoCriaturas.Obter(CatalogoCriaturas.GuardiaoDoReflexo);
        var raro = CatalogoCriaturas.Obter(CatalogoCriaturas.PresaVelha);

        Assert.Equal(CategoriaDeCriatura.Chefe, chefe.Categoria);
        Assert.Equal(1.0, chefe.ChanceDeAparecer);
        Assert.Equal(0, chefe.IntervaloDeAparicaoSegundos);

        Assert.Equal(CategoriaDeCriatura.ChefeRaro, raro.Categoria);
        Assert.Equal(0.2, raro.ChanceDeAparecer);
        Assert.Equal(1800, raro.IntervaloDeAparicaoSegundos);
    }

    [Fact]
    public void Toda_criatura_respeita_as_regras_da_sua_categoria()
    {
        Assert.All(
            CatalogoCriaturas.Todas,
            criatura =>
            {
                Assert.False(string.IsNullOrWhiteSpace(criatura.Nome));
                Assert.True(criatura.Nivel > 0);
                Assert.True(criatura.VidaBase > 0);
                Assert.True(criatura.DanoBase > 0);
                Assert.True(criatura.ExperienciaBase > 0);

                switch (criatura.Categoria)
                {
                    case CategoriaDeCriatura.Comum:
                        Assert.Equal(1.0, criatura.ChanceDeAparecer);
                        Assert.InRange(criatura.Id, 1, 99);
                        break;
                    case CategoriaDeCriatura.Chefe:
                        Assert.Equal(1.0, criatura.ChanceDeAparecer);
                        Assert.InRange(criatura.Id, 100, 199);
                        break;
                    case CategoriaDeCriatura.ChefeRaro:
                        Assert.InRange(criatura.ChanceDeAparecer, 0.0001, 0.9999);
                        Assert.True(criatura.IntervaloDeAparicaoSegundos > 0);
                        Assert.InRange(criatura.Id, 200, 299);
                        break;
                    default:
                        Assert.Fail($"Categoria sem regra no teste: {criatura.Categoria}");
                        break;
                }
            }
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void Obter_devolve_null_fora_do_catalogo(int criaturaId) =>
        Assert.Null(CatalogoCriaturas.Obter(criaturaId));
}
