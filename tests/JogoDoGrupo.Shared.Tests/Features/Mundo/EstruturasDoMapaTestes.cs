using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class EstruturasDoMapaTestes
{
    [Fact]
    public void A_leva_tem_os_quatro_tipos()
    {
        Assert.Equal(4, EstruturasDoMapa.Lugares.Length);
        Assert.Contains(EstruturasDoMapa.Lugares, lugar => lugar.Tipo == TipoDeEstrutura.Ruina);
        Assert.Contains(
            EstruturasDoMapa.Lugares,
            lugar => lugar.Tipo == TipoDeEstrutura.Acampamento
        );
        Assert.Contains(EstruturasDoMapa.Lugares, lugar => lugar.Tipo == TipoDeEstrutura.Passagem);
        Assert.Contains(EstruturasDoMapa.Lugares, lugar => lugar.Tipo == TipoDeEstrutura.Marco);
    }

    [Fact]
    public void Cada_lugar_cai_no_bioma_previsto_com_a_base_no_chao()
    {
        foreach (var lugar in EstruturasDoMapa.Lugares)
        {
            Assert.NotEmpty(lugar.Pecas);
            foreach (var peca in lugar.Pecas)
            {
                Assert.True(
                    lugar.Bioma == MapaDoMundo.BiomaEm(peca.X, peca.Z),
                    $"{lugar.Nome} em ({peca.X}, {peca.Z}) caiu em {MapaDoMundo.BiomaEm(peca.X, peca.Z)}."
                );
                Assert.Equal(MapaDoMundo.AlturaEm(peca.X, peca.Z), peca.Altura);
                Assert.True(EstruturasDoMapa.ForaDaCidade(peca.X, peca.Z), lugar.Nome);
                Assert.True(EstruturasDoMapa.ChaoSeco(peca.X, peca.Z), lugar.Nome);
                Assert.True(EstruturasDoMapa.EncostaAndavel(peca.X, peca.Z), lugar.Nome);
                Assert.NotEqual(Bioma.Cidade, MapaDoMundo.BiomaEm(peca.X, peca.Z));
                Assert.NotEqual(Bioma.Lava, MapaDoMundo.BiomaEm(peca.X, peca.Z));
                Assert.NotEqual(Bioma.Pantano, MapaDoMundo.BiomaEm(peca.X, peca.Z));
            }
        }
    }
}
