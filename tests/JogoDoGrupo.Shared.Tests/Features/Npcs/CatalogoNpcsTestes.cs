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
        Assert.Equal(21, cidadeNpcs.Length);

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

        var cuidador = CatalogoNpcs.Obter(CatalogoNpcs.IdCuidadorDePets);
        Assert.NotNull(cuidador);
        Assert.Equal("Cuidadora Mirna", cuidador.Nome);
        Assert.Equal(PapelNpc.CuidadorDePets, cuidador.Papel);

        var leiloeiro = CatalogoNpcs.Obter(CatalogoNpcs.IdLeiloeiro);
        Assert.NotNull(leiloeiro);
        Assert.Equal("Leiloeiro Dumas", leiloeiro.Nome);
        Assert.Equal(PapelNpc.Leiloeiro, leiloeiro.Papel);

        var valeria = CatalogoNpcs.Obter(CatalogoNpcs.IdValeria);
        Assert.NotNull(valeria);
        Assert.Equal("Capitã Valéria", valeria.Nome);
        Assert.Equal(PapelNpc.Guarda, valeria.Papel);

        var guardaNorte = CatalogoNpcs.Obter(CatalogoNpcs.IdGuardaNorte);
        Assert.NotNull(guardaNorte);
        Assert.Equal("Guarda Kael", guardaNorte.Nome);
        Assert.Equal(PapelNpc.Guarda, guardaNorte.Papel);

        var quadro = CatalogoNpcs.Obter(CatalogoNpcs.IdQuadroDeDiarias);
        Assert.NotNull(quadro);
        Assert.Equal("Quadro de Diárias", quadro.Nome);
        Assert.Equal(PapelNpc.DadorDeMissao, quadro.Papel);

        var elric = CatalogoNpcs.Obter(CatalogoNpcs.IdMoradorElric);
        Assert.NotNull(elric);
        Assert.Equal("Morador Elric", elric.Nome);
        Assert.Equal(PapelNpc.Morador, elric.Papel);

        var lyra = CatalogoNpcs.Obter(CatalogoNpcs.IdMoradoraLyra);
        Assert.NotNull(lyra);
        Assert.Equal("Moradora Lyra", lyra.Nome);
        Assert.Equal(PapelNpc.Morador, lyra.Papel);

        var lenhador = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreLenhador);
        Assert.NotNull(lenhador);
        Assert.Equal("Lenhador Garrido", lenhador.Nome);
        Assert.Equal(PapelNpc.MestreLenhador, lenhador.Papel);

        var minerador = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreMinerador);
        Assert.NotNull(minerador);
        Assert.Equal("Mineradora Petra", minerador.Nome);
        Assert.Equal(PapelNpc.MestreMinerador, minerador.Papel);

        var curtidor = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreCurtidor);
        Assert.NotNull(curtidor);
        Assert.Equal("Curtidora Sela", curtidor.Nome);
        Assert.Equal(PapelNpc.MestreCurtidor, curtidor.Papel);

        var mestreFerreiro = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreFerreiro);
        Assert.NotNull(mestreFerreiro);
        Assert.Equal("Mestre Ferreiro Torvin", mestreFerreiro.Nome);
        Assert.Equal(PapelNpc.MestreFerreiro, mestreFerreiro.Papel);

        var alquimista = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreAlquimista);
        Assert.NotNull(alquimista);
        Assert.Equal("Alquimista Zéfira", alquimista.Nome);
        Assert.Equal(PapelNpc.MestreAlquimista, alquimista.Papel);

        var cozinheiro = CatalogoNpcs.Obter(CatalogoNpcs.IdMestreCozinheiro);
        Assert.NotNull(cozinheiro);
        Assert.Equal("Cozinheira Cota", cozinheiro.Nome);
        Assert.Equal(PapelNpc.MestreCozinheiro, cozinheiro.Papel);
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

        var inexistente = CatalogoNpcs.PorPapel(PapelNpc.Estalajadeiro);
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
