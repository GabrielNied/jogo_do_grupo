using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Pets;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasLootCompanheirosTestes
{
    [Fact]
    public void Itens_de_pet_mapeiam_para_especie()
    {
        Assert.Equal(
            EspecieDePet.CervoJovem,
            RegrasPets.EspecieDoItem(CatalogoItens.SinoDoCervoJovem)
        );
        Assert.Equal(
            EspecieDePet.FilhoteDeJavali,
            RegrasPets.EspecieDoItem(CatalogoItens.SinoDoFilhoteDeJavali)
        );
    }

    [Fact]
    public void Redeas_do_lobo_de_presa_mapeiam_tipo_tres()
    {
        Assert.Equal(
            RegrasMontaria.TipoLoboDePresa,
            RegrasMontaria.TipoMontariaDoItem(CatalogoItens.RedeasDoLoboDePresa)
        );
    }

    [Fact]
    public void Javali_sorteia_entre_filhote_e_coelho()
    {
        var sorteador = new Random(1);
        var vistos = new HashSet<int>();
        for (var i = 0; i < 200; i++)
        {
            vistos.Add(
                RegrasLootCompanheiros.SortearItemDePet(CatalogoCriaturas.JavaliDoCampo, sorteador)
            );
        }

        Assert.Contains(CatalogoItens.SinoDoFilhoteDeJavali, vistos);
        Assert.Contains(CatalogoItens.SinoDoCoelhoDoPasto, vistos);
    }

    [Fact]
    public void Chefe_sem_montaria_cadastrada_nao_retorna_item()
    {
        Assert.Equal(
            0,
            RegrasLootCompanheiros.ItemDeMontariaDoChefe(CatalogoCriaturas.JavaliDoCampo)
        );
    }
}
