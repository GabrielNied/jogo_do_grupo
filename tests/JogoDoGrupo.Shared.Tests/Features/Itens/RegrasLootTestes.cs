using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasLootTestes
{
    [Fact]
    public void Valores_padrao_de_balanceamento()
    {
        Assert.Equal(0.5, RegrasLoot.ChanceDeNenhumItem);
        Assert.Equal(0.4, RegrasLoot.ChanceDeUmItem);
        Assert.Equal(0.1, RegrasLoot.ChanceDeDoisItens);
        Assert.Equal(2, RegrasLoot.MaximoPorInimigo);
        Assert.Equal(180, RegrasLoot.ReservaSegundos);
        Assert.Equal(3.0, RegrasLoot.RaioDeColetaMetros);
        Assert.Equal(12.0, RegrasLoot.RaioDeVisaoMetros);
    }

    [Fact]
    public void As_tres_chances_somam_um()
    {
        var soma =
            RegrasLoot.ChanceDeNenhumItem
            + RegrasLoot.ChanceDeUmItem
            + RegrasLoot.ChanceDeDoisItens;
        Assert.Equal(1.0, soma, 10);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.25, 0)]
    [InlineData(0.4999, 0)]
    [InlineData(0.5, 1)] // a faixa de um item começa exatamente no fim da de nenhum
    [InlineData(0.75, 1)]
    [InlineData(0.8999, 1)]
    [InlineData(0.95, 2)]
    [InlineData(0.9999, 2)]
    public void QuantidadeSorteada_segue_as_faixas(double sorteio, int esperado) =>
        Assert.Equal(esperado, RegrasLoot.QuantidadeSorteada(sorteio));

    [Theory]
    [InlineData(-1.0, 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(1.0, 2)]
    [InlineData(5.0, 2)]
    public void QuantidadeSorteada_fora_da_faixa_usa_a_ponta(double sorteio, int esperado) =>
        Assert.Equal(esperado, RegrasLoot.QuantidadeSorteada(sorteio));

    [Fact]
    public void QuantidadeSorteada_nunca_passa_do_maximo()
    {
        for (var i = 0; i < 1000; i++)
        {
            var quantidade = RegrasLoot.QuantidadeSorteada(i / 1000.0);
            Assert.InRange(quantidade, 0, RegrasLoot.MaximoPorInimigo);
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 0, true)] // em cima do item
    [InlineData(0, 0, 3, 0, true)] // exatamente no raio
    [InlineData(0, 0, 0, -3, true)]
    [InlineData(0, 0, 3.01, 0, false)]
    [InlineData(10, 10, 12, 12, true)] // 2,83 m na diagonal
    [InlineData(10, 10, 12.2, 12.2, false)] // 3,11 m na diagonal
    [InlineData(0, 0, 3, 3, false)] // cada eixo no limite, mas 4,24 m de distância
    public void DentroDoRaioDeColeta_mede_no_plano_do_chao(
        double px,
        double pz,
        double lx,
        double lz,
        bool esperado
    ) => Assert.Equal(esperado, RegrasLoot.DentroDoRaioDeColeta(px, pz, lx, lz));

    [Theory]
    [InlineData(0.0, 180)]
    [InlineData(0.1, 180)] // arredonda para cima
    [InlineData(1.0, 179)]
    [InlineData(179.5, 1)]
    [InlineData(180.0, 0)]
    [InlineData(500.0, 0)]
    [InlineData(-5.0, 180)] // relógio que andou para trás não aumenta o prazo
    public void SegundosRestantes_arredonda_para_cima_dentro_do_prazo(
        double decorrido,
        int esperado
    ) => Assert.Equal(esperado, RegrasLoot.SegundosRestantes(decorrido));

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(179.999, false)]
    [InlineData(180.0, true)]
    [InlineData(181.0, true)]
    public void Expirou_so_ao_completar_a_reserva(double decorrido, bool esperado) =>
        Assert.Equal(esperado, RegrasLoot.Expirou(decorrido));
}
