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
        Assert.Equal(2, CatalogoCriaturas.LoboDoCampo);
        Assert.Equal(3, CatalogoCriaturas.TatuRochoso);
        Assert.Equal(4, CatalogoCriaturas.CervoDeGalhadaOca);
        Assert.Equal(5, CatalogoCriaturas.MariposaDeBreu);
        Assert.Equal(6, CatalogoCriaturas.LoboDeGeada);
        Assert.Equal(7, CatalogoCriaturas.CarapacaDeBrasa);
        Assert.Equal(8, CatalogoCriaturas.JavaliFerrugem);
        Assert.Equal(9, CatalogoCriaturas.MorcegoDasSombras);
        Assert.Equal(10, CatalogoCriaturas.ArqueiroDoCampo);
        Assert.Equal(300, CatalogoCriaturas.AnciaoTrovejante);
        Assert.Equal(301, CatalogoCriaturas.ColossoBasaltico);
        Assert.Equal(
            new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10, 100, 200, 300, 301 },
            CatalogoCriaturas.Todas.Select(c => c.Id).ToArray()
        );
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

    [Theory]
    [InlineData(CatalogoCriaturas.JavaliFerrugem, "Javali Ferrugem", 3, 180, 18)]
    [InlineData(CatalogoCriaturas.LoboDeGeada, "Lobo de Geada", 22, 1400, 95)]
    [InlineData(CatalogoCriaturas.CarapacaDeBrasa, "Carapaça de Brasa", 30, 3200, 180)]
    [InlineData(CatalogoCriaturas.CervoDeGalhadaOca, "Cervo de Galhada Oca", 12, 650, 22)]
    [InlineData(CatalogoCriaturas.MariposaDeBreu, "Mariposa de Breu", 10, 90, 14)]
    public void Especies_do_livro_tem_a_ficha_fixa_de_comum(
        int id,
        string nome,
        int nivel,
        int vida,
        int dano
    )
    {
        var ficha = CatalogoCriaturas.Obter(id);

        Assert.Equal(nome, ficha.Nome);
        Assert.Equal(CategoriaDeCriatura.Comum, ficha.Categoria);
        Assert.Equal(nivel, ficha.Nivel);
        Assert.Equal(vida, ficha.VidaBase);
        Assert.Equal(dano, ficha.DanoBase);
        Assert.Equal(1.0, ficha.ChanceDeAparecer);
    }

    [Fact]
    public void Javali_ferrugem_convive_com_o_javali_do_campo_sem_mexer_nele()
    {
        var campo = CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliDoCampo);
        var ferrugem = CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliFerrugem);

        Assert.Equal(40, campo.DanoBase);
        Assert.Equal(campo.Nivel, ferrugem.Nivel);
        Assert.Equal(campo.VidaBase, ferrugem.VidaBase);
        Assert.True(ferrugem.DanoBase < campo.DanoBase);
    }

    [Theory]
    [InlineData(CatalogoCriaturas.AnciaoTrovejante, "Ancião Trovejante", 45, 250_000)]
    [InlineData(CatalogoCriaturas.ColossoBasaltico, "Colosso Basáltico", 50, 350_000)]
    public void Chefes_de_mundo_tem_os_numeros_do_livro_e_so_aparecem_por_sorteio(
        int id,
        string nome,
        int nivel,
        int vida
    )
    {
        var ficha = CatalogoCriaturas.Obter(id);

        Assert.Equal(nome, ficha.Nome);
        Assert.Equal(CategoriaDeCriatura.ChefeDeMundo, ficha.Categoria);
        Assert.Equal(nivel, ficha.Nivel);
        Assert.Equal(vida, ficha.VidaBase);
        Assert.InRange(ficha.ChanceDeAparecer, 0.0001, 0.9999);
        Assert.True(RegrasCriaturas.AvisaNaArea(ficha.Categoria));
        Assert.False(RegrasCriaturas.PodeSerElite(ficha.Categoria));
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
                    case CategoriaDeCriatura.ChefeDeMundo:
                        Assert.InRange(criatura.ChanceDeAparecer, 0.0001, 0.9999);
                        Assert.True(criatura.IntervaloDeAparicaoSegundos > 0);
                        Assert.InRange(criatura.Id, 300, 399);
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
    [InlineData(9)]
    [InlineData(-1)]
    public void Obter_devolve_null_fora_do_catalogo(int criaturaId) =>
        Assert.Null(CatalogoCriaturas.Obter(criaturaId));
}
