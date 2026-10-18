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
        Assert.Equal(3, cidadeNpcs.Length);

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

        Assert.True(ConfigNpcEconomia.DentroDoRaioVendedor(vendedor.X, vendedor.Z));
        Assert.True(ConfigNpcEconomia.DentroDoRaioFerreiro(ferreiro.X, ferreiro.Z));
        Assert.False(ConfigNpcEconomia.DentroDoRaioVendedor(100f, 100f));
    }
}
