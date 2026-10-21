using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasVisualEquipadoTestes
{
    [Theory]
    [InlineData(PosicaoEquipamento.Arma1, 1, EncaixeDoVisual.MaoDireita)]
    [InlineData(PosicaoEquipamento.Arma1, 2, EncaixeDoVisual.Costas)]
    [InlineData(PosicaoEquipamento.Arma2, 1, EncaixeDoVisual.Costas)]
    [InlineData(PosicaoEquipamento.Arma2, 2, EncaixeDoVisual.MaoDireita)]
    public void ArmaAtivaVaiAMaoEAInativaAsCostas(
        PosicaoEquipamento posicao,
        int ativo,
        EncaixeDoVisual esperado
    ) => Assert.Equal(esperado, RegrasVisualEquipado.EncaixeDe(posicao, ativo));

    [Theory]
    [InlineData(PosicaoEquipamento.Secundaria1)]
    [InlineData(PosicaoEquipamento.Peito)]
    [InlineData(PosicaoEquipamento.Anel1)]
    public void SoArmaPrincipalTemModelo(PosicaoEquipamento posicao) =>
        Assert.Null(RegrasVisualEquipado.EncaixeDe(posicao, 1));

    [Fact]
    public void Montar_TrazSoArmasPrincipaisEOrdenaPorPosicao()
    {
        EntradaInventario Peca(PosicaoEquipamento posicao, TipoEquipamento tipo) =>
            new EntradaInventario
            {
                Posicao = posicao,
                Equipamento = new Equipamento { ModeloId = 100, Tipo = tipo },
            };

        var visual = RegrasVisualEquipado.Montar(
            7,
            2,
            new[]
            {
                Peca(PosicaoEquipamento.Peito, TipoEquipamento.Peito),
                Peca(PosicaoEquipamento.Arma2, TipoEquipamento.Arco),
                Peca(PosicaoEquipamento.Secundaria1, TipoEquipamento.Escudo),
                Peca(PosicaoEquipamento.Arma1, TipoEquipamento.Espada),
            }
        );

        Assert.Equal(7, visual.PersonagemId);
        Assert.Equal(2, visual.ConjuntoAtivo);
        Assert.Equal(
            new[] { PosicaoEquipamento.Arma1, PosicaoEquipamento.Arma2 },
            visual.Armas.Select(a => a.Posicao)
        );
    }

    [Fact]
    public void NomesDosEncaixes_SaoOsDoVisualDoPersonagem()
    {
        Assert.Equal(
            "EncaixeMaoDireita",
            RegrasVisualEquipado.NomeDoEncaixe(EncaixeDoVisual.MaoDireita)
        );
        Assert.Equal("EncaixeCostas", RegrasVisualEquipado.NomeDoEncaixe(EncaixeDoVisual.Costas));
        Assert.Equal("EncaixeCabeca", RegrasVisualEquipado.NomeDoEncaixe(EncaixeDoVisual.Cabeca));
        Assert.Equal("EncaixeTronco", RegrasVisualEquipado.NomeDoEncaixe(EncaixeDoVisual.Tronco));
    }

    [Theory]
    [InlineData(PosicaoEquipamento.Capacete, EncaixeDoVisual.Cabeca)]
    [InlineData(PosicaoEquipamento.Peito, EncaixeDoVisual.Tronco)]
    public void ArmaduraTemEncaixeDefinido(PosicaoEquipamento posicao, EncaixeDoVisual esperado) =>
        Assert.Equal(esperado, RegrasVisualEquipado.EncaixeArmaduraDe(posicao));

    [Theory]
    [InlineData(PosicaoEquipamento.Luvas)]
    [InlineData(PosicaoEquipamento.Calca)]
    [InlineData(PosicaoEquipamento.Botas)]
    [InlineData(PosicaoEquipamento.Anel1)]
    public void OutrasPosicoes_NaoTemEncaixeArmadura(PosicaoEquipamento posicao) =>
        Assert.Null(RegrasVisualEquipado.EncaixeArmaduraDe(posicao));

    [Fact]
    public void Montar_TrazArmasEArmadurasOrdenadasPorPosicao()
    {
        EntradaInventario Peca(PosicaoEquipamento posicao, TipoEquipamento tipo) =>
            new EntradaInventario
            {
                Posicao = posicao,
                Equipamento = new Equipamento { ModeloId = 100, Tipo = tipo },
            };

        var visual = RegrasVisualEquipado.Montar(
            7,
            2,
            new[]
            {
                Peca(PosicaoEquipamento.Peito, TipoEquipamento.Peito),
                Peca(PosicaoEquipamento.Capacete, TipoEquipamento.Capacete),
                Peca(PosicaoEquipamento.Arma2, TipoEquipamento.Arco),
                Peca(PosicaoEquipamento.Secundaria1, TipoEquipamento.Escudo),
                Peca(PosicaoEquipamento.Arma1, TipoEquipamento.Espada),
            }
        );

        Assert.Equal(7, visual.PersonagemId);
        Assert.Equal(2, visual.ConjuntoAtivo);
        Assert.Equal(
            new[] { PosicaoEquipamento.Arma1, PosicaoEquipamento.Arma2 },
            visual.Armas.Select(a => a.Posicao)
        );
        Assert.Equal(
            new[] { PosicaoEquipamento.Capacete, PosicaoEquipamento.Peito },
            visual.Armaduras.Select(a => a.Posicao)
        );
    }

    [Fact]
    public void Montar_CalculaTierDeCadaArma()
    {
        var espada = new Equipamento
        {
            ModeloId = 173,
            Tipo = TipoEquipamento.Espada,
            Atributos = new[]
            {
                new AtributoItem { Status = StatusItem.DanoFisico, Valor = 100 },
            },
        };

        var visual = RegrasVisualEquipado.Montar(
            1,
            1,
            new[]
            {
                new EntradaInventario { Posicao = PosicaoEquipamento.Arma1, Equipamento = espada },
            }
        );

        Assert.Single(visual.Armas);
        Assert.True(visual.Armas[0].Tier >= TierItem.A);
    }
}
