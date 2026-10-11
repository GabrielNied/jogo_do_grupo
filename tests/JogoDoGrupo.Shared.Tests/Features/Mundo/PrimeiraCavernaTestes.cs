using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class PrimeiraCavernaTestes
{
    [Fact]
    public void A_boca_fica_nas_colinas_apoiada_no_chao()
    {
        Assert.Equal(
            Bioma.Colinas,
            MapaDoMundo.BiomaEm(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ)
        );
        Assert.NotEqual(
            Bioma.Cidade,
            MapaDoMundo.BiomaEm(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ)
        );
        Assert.True(
            EstruturasDoMapa.ForaDaCidade(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ)
        );
        Assert.True(EstruturasDoMapa.ChaoSeco(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ));
        Assert.Equal(
            MapaDoMundo.AlturaEm(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ),
            PrimeiraCaverna.EntradaY
        );
    }

    [Fact]
    public void Entrar_chega_no_interior_e_sair_devolve_a_boca()
    {
        Assert.True(PrimeiraCaverna.NaBoca(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ));
        Assert.False(
            PrimeiraCaverna.NaBoca(PrimeiraCaverna.InteriorX, PrimeiraCaverna.NascimentoZ)
        );
        Assert.True(PrimeiraCaverna.NaSaida(PrimeiraCaverna.InteriorX, PrimeiraCaverna.InteriorZ));
        Assert.False(
            PrimeiraCaverna.NaSaida(PrimeiraCaverna.InteriorX, PrimeiraCaverna.NascimentoZ)
        );
        Assert.True(PrimeiraCaverna.FolgaDoNascimento() > 0.35f);
    }
}
