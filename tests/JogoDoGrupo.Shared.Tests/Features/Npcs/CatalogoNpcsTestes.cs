using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs;

public class CatalogoNpcsTestes
{
    [Fact]
    public void CatalogoPadrao_ContemNpcsDaCidade()
    {
        Assert.NotEmpty(CatalogoNpcs.Todos);
        var cidadeNpcs = CatalogoNpcs.PorArea(CatalogoNpcs.AreaCidade);
        Assert.Equal(8, cidadeNpcs.Length);

        var vendedor = CatalogoNpcs.Obter(CatalogoNpcs.IdVendedor);
        Assert.NotNull(vendedor);
        Assert.Equal("Mercador Alden", vendedor.Nome);
        Assert.Equal(PapelNpc.Vendedor, vendedor.Papel);

        var ferreiro = CatalogoNpcs.Obter(CatalogoNpcs.IdFerreiro);
        Assert.NotNull(ferreiro);
        Assert.Equal("Ferreiro Brom", ferreiro.Nome);
        Assert.Equal(PapelNpc.Ferreiro, ferreiro.Papel);

        var banqueiro = CatalogoNpcs.Obter(CatalogoNpcs.IdBanqueiro);
        Assert.NotNull(banqueiro);
        Assert.Equal("Banqueiro Cassian", banqueiro.Nome);
        Assert.Equal(PapelNpc.Banqueiro, banqueiro.Papel);

        var armeiro = CatalogoNpcs.Obter(CatalogoNpcs.IdArmeiro);
        Assert.NotNull(armeiro);
        Assert.Equal("Armeiro Gareth", armeiro.Nome);
        Assert.Equal(PapelNpc.Armeiro, armeiro.Papel);

        var armadureiro = CatalogoNpcs.Obter(CatalogoNpcs.IdArmadureiro);
        Assert.NotNull(armadureiro);
        Assert.Equal("Armadureira Bruna", armadureiro.Nome);
        Assert.Equal(PapelNpc.Armadureiro, armadureiro.Papel);

        var taverneiro = CatalogoNpcs.Obter(CatalogoNpcs.IdTaverneiro);
        Assert.NotNull(taverneiro);
        Assert.Equal("Taverneiro Barnabé", taverneiro.Nome);
        Assert.Equal(PapelNpc.Taverneiro, taverneiro.Papel);

        var merceeiro = CatalogoNpcs.Obter(CatalogoNpcs.IdMerceeiro);
        Assert.NotNull(merceeiro);
        Assert.Equal("Merceeiro Tomas", merceeiro.Nome);
        Assert.Equal(PapelNpc.Merceeiro, merceeiro.Papel);

        var cavalarico = CatalogoNpcs.Obter(CatalogoNpcs.IdCavalarico);
        Assert.NotNull(cavalarico);
        Assert.Equal("Cavalariço Roderic", cavalarico.Nome);
        Assert.Equal(PapelNpc.Cavalarico, cavalarico.Papel);
    }

    [Fact]
    public void Obter_ComIdInvalido_RetornaNulo()
    {
        var npc = CatalogoNpcs.Obter(9999);
        Assert.Null(npc);
    }

    [Fact]
    public void PorArea_ComAreaInexistente_RetornaVazio()
    {
        var npcs = CatalogoNpcs.PorArea("area_inexistente");
        Assert.Empty(npcs);
    }

    [Fact]
    public void PorPapel_RetornaNpcEsperado()
    {
        var vendedor = CatalogoNpcs.PorPapel(PapelNpc.Vendedor);
        Assert.NotNull(vendedor);
        Assert.Equal(CatalogoNpcs.IdVendedor, vendedor.Id);

        var inexistente = CatalogoNpcs.PorPapel(PapelNpc.DadorDeMissao);
        Assert.Null(inexistente);
    }

    [Fact]
    public void DentroDoAlcance_VerificaRaioDeInteracao()
    {
        var vendedor = CatalogoNpcs.Vendedor;

        // Bem perto do NPC
        Assert.True(CatalogoNpcs.DentroDoAlcance(CatalogoNpcs.IdVendedor, vendedor.X, vendedor.Z));
        Assert.True(
            CatalogoNpcs.DentroDoAlcance(CatalogoNpcs.IdVendedor, vendedor.X + 1f, vendedor.Z)
        );

        // Fora do raio de interação
        Assert.False(
            CatalogoNpcs.DentroDoAlcance(
                CatalogoNpcs.IdVendedor,
                vendedor.X + vendedor.RaioInteracao + 2f,
                vendedor.Z
            )
        );
    }

    [Fact]
    public void ConfigNpcEconomia_AlinhadoAoCatalogoNpcs()
    {
        var vendedor = CatalogoNpcs.Vendedor;
        var ferreiro = CatalogoNpcs.Ferreiro;

        Assert.Equal(vendedor.X, ConfigNpcEconomia.VendedorX);
        Assert.Equal(vendedor.Z, ConfigNpcEconomia.VendedorZ);
        Assert.Equal(ferreiro.X, ConfigNpcEconomia.FerreiroX);
        Assert.Equal(ferreiro.Z, ConfigNpcEconomia.FerreiroZ);
        Assert.Equal(CatalogoNpcs.Armeiro.X, ConfigNpcEconomia.ArmeiroX);
        Assert.Equal(CatalogoNpcs.Armeiro.Z, ConfigNpcEconomia.ArmeiroZ);
        Assert.Equal(CatalogoNpcs.Armadureiro.X, ConfigNpcEconomia.ArmadureiroX);
        Assert.Equal(CatalogoNpcs.Armadureiro.Z, ConfigNpcEconomia.ArmadureiroZ);
        Assert.Equal(CatalogoNpcs.Taverneiro.X, ConfigNpcEconomia.TaverneiroX);
        Assert.Equal(CatalogoNpcs.Taverneiro.Z, ConfigNpcEconomia.TaverneiroZ);
        Assert.Equal(CatalogoNpcs.Merceeiro.X, ConfigNpcEconomia.MerceeiroX);
        Assert.Equal(CatalogoNpcs.Merceeiro.Z, ConfigNpcEconomia.MerceeiroZ);

        Assert.True(ConfigNpcEconomia.DentroDoRaioVendedor(vendedor.X, vendedor.Z));
        Assert.True(ConfigNpcEconomia.DentroDoRaioFerreiro(ferreiro.X, ferreiro.Z));
        Assert.True(
            ConfigNpcEconomia.DentroDoRaioArmeiro(CatalogoNpcs.Armeiro.X, CatalogoNpcs.Armeiro.Z)
        );
        Assert.True(
            ConfigNpcEconomia.DentroDoRaioArmadureiro(
                CatalogoNpcs.Armadureiro.X,
                CatalogoNpcs.Armadureiro.Z
            )
        );
        Assert.True(
            ConfigNpcEconomia.DentroDoRaioTaverneiro(
                CatalogoNpcs.Taverneiro.X,
                CatalogoNpcs.Taverneiro.Z
            )
        );
        Assert.True(
            ConfigNpcEconomia.DentroDoRaioMerceeiro(
                CatalogoNpcs.Merceeiro.X,
                CatalogoNpcs.Merceeiro.Z
            )
        );
        Assert.True(
            ConfigNpcEconomia.DentroDoRaioLoja(
                CategoriaLoja.Armeiro,
                CatalogoNpcs.Armeiro.X,
                CatalogoNpcs.Armeiro.Z
            )
        );
        Assert.False(
            ConfigNpcEconomia.DentroDoRaioLoja(
                CategoriaLoja.Armeiro,
                CatalogoNpcs.Taverneiro.X,
                CatalogoNpcs.Taverneiro.Z
            )
        );
        Assert.False(ConfigNpcEconomia.DentroDoRaioVendedor(100f, 100f));
    }
}
