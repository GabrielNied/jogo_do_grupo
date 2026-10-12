using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class EncaixeAutomaticoTestes
{
    private static readonly PosicaoEquipamento[] Nenhum = [];

    [Theory]
    [InlineData(TipoEquipamento.Capacete, PosicaoEquipamento.Capacete)]
    [InlineData(TipoEquipamento.Peito, PosicaoEquipamento.Peito)]
    [InlineData(TipoEquipamento.Luvas, PosicaoEquipamento.Luvas)]
    [InlineData(TipoEquipamento.Calca, PosicaoEquipamento.Calca)]
    [InlineData(TipoEquipamento.Botas, PosicaoEquipamento.Botas)]
    [InlineData(TipoEquipamento.Cinto, PosicaoEquipamento.Cinto)]
    [InlineData(TipoEquipamento.Adorno, PosicaoEquipamento.Adorno)]
    [InlineData(TipoEquipamento.Anel, PosicaoEquipamento.Anel1)]
    [InlineData(TipoEquipamento.Brinco, PosicaoEquipamento.Brinco1)]
    public void PecaFixaOuPrimeiroLivre(TipoEquipamento tipo, PosicaoEquipamento esperado)
    {
        Assert.Equal(esperado, RegrasEquipamentos.EncaixeAutomatico(tipo, Nenhum, 1));
        Assert.True(RegrasEquipamentos.Aceita(tipo, esperado));
    }

    [Fact]
    public void AnelEBrincoVaoParaOSegundoQuandoOPrimeiroEstaOcupado()
    {
        Assert.Equal(
            PosicaoEquipamento.Anel2,
            RegrasEquipamentos.EncaixeAutomatico(
                TipoEquipamento.Anel,
                [PosicaoEquipamento.Anel1],
                1
            )
        );
        Assert.Equal(
            PosicaoEquipamento.Brinco2,
            RegrasEquipamentos.EncaixeAutomatico(
                TipoEquipamento.Brinco,
                [PosicaoEquipamento.Brinco1],
                1
            )
        );
    }

    [Fact]
    public void AnelEBrincoCom1LivreEOutroOcupadoUsaOPrimeiro()
    {
        Assert.Equal(
            PosicaoEquipamento.Anel1,
            RegrasEquipamentos.EncaixeAutomatico(
                TipoEquipamento.Anel,
                [PosicaoEquipamento.Anel2],
                1
            )
        );
    }

    [Fact]
    public void AnelEBrincoComOsDoisOcupadosTrocaOPrimeiro()
    {
        Assert.Equal(
            PosicaoEquipamento.Anel1,
            RegrasEquipamentos.EncaixeAutomatico(
                TipoEquipamento.Anel,
                [PosicaoEquipamento.Anel1, PosicaoEquipamento.Anel2],
                1
            )
        );
        Assert.Equal(
            PosicaoEquipamento.Brinco1,
            RegrasEquipamentos.EncaixeAutomatico(
                TipoEquipamento.Brinco,
                [PosicaoEquipamento.Brinco1, PosicaoEquipamento.Brinco2],
                2
            )
        );
    }

    [Theory]
    [InlineData(TipoEquipamento.Espada, 1, PosicaoEquipamento.Arma1)]
    [InlineData(TipoEquipamento.Martelo, 2, PosicaoEquipamento.Arma2)]
    [InlineData(TipoEquipamento.Arco, 1, PosicaoEquipamento.Arma1)]
    [InlineData(TipoEquipamento.Cajado, 2, PosicaoEquipamento.Arma2)]
    [InlineData(TipoEquipamento.Escudo, 1, PosicaoEquipamento.Secundaria1)]
    [InlineData(TipoEquipamento.Foco, 2, PosicaoEquipamento.Secundaria2)]
    public void ArmaVaiParaOConjuntoAtivo(
        TipoEquipamento tipo,
        int conjunto,
        PosicaoEquipamento esperado
    )
    {
        Assert.Equal(esperado, RegrasEquipamentos.EncaixeAutomatico(tipo, Nenhum, conjunto));
        Assert.True(RegrasEquipamentos.Aceita(tipo, esperado));
    }

    [Fact]
    public void CapacidadeDaBolsaConfereAbaECasa()
    {
        var capacidade = new CapacidadeBolsa { Abas = 2, CasasPorAba = 40 };
        Assert.Equal(80, capacidade.Total);
        Assert.True(capacidade.Contem(1, 39));
        Assert.False(capacidade.Contem(2, 0));
        Assert.False(capacidade.Contem(0, 40));
        Assert.False(capacidade.Contem(-1, 0));
        Assert.False(capacidade.Contem(0, RegrasItens.SemCasa));
    }
}
