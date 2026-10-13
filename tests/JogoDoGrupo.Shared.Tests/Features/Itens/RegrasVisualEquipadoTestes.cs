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
    }
}
