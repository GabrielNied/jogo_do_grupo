using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasEquipamentosTestes
{
    [Fact]
    public void Livro_TemOnzePosicoesEDoisConjuntos()
    {
        Assert.Equal(16, Enum.GetValues<PosicaoEquipamento>().Length);
        Assert.True(RegrasEquipamentos.Aceita(TipoEquipamento.Anel, PosicaoEquipamento.Anel1));
        Assert.True(RegrasEquipamentos.Aceita(TipoEquipamento.Anel, PosicaoEquipamento.Anel2));
        Assert.True(RegrasEquipamentos.Aceita(TipoEquipamento.Brinco, PosicaoEquipamento.Brinco2));
        Assert.False(RegrasEquipamentos.Aceita(TipoEquipamento.Anel, PosicaoEquipamento.Capacete));
    }

    [Theory]
    [InlineData(RaridadeItem.Comum, 10, 1)]
    [InlineData(RaridadeItem.Incomum, 15, 2)]
    [InlineData(RaridadeItem.Raro, 20, 3)]
    [InlineData(RaridadeItem.Epico, 25, 4)]
    [InlineData(RaridadeItem.Lendario, 30, 5)]
    public void Raridade_UsaATabelaDoLivro(RaridadeItem raridade, int limite, int skills)
    {
        Assert.Equal(limite, RegrasEquipamentos.NivelMaximo(raridade));
        Assert.Equal(limite, RegrasEquipamentos.LimiteInfusoes(raridade));
        Assert.Equal(skills, RegrasEquipamentos.SlotsSkills(raridade));
    }

    [Fact]
    public void Totais_SomamOsDoisAneisMasSomenteAArmaAtiva()
    {
        EntradaInventario Item(PosicaoEquipamento p, double valor) =>
            new()
            {
                Posicao = p,
                Equipamento = new Equipamento
                {
                    Atributos = new[]
                    {
                        new AtributoItem { Status = StatusItem.Forca, Valor = valor },
                    },
                },
            };
        var itens = new[]
        {
            Item(PosicaoEquipamento.Anel1, 2),
            Item(PosicaoEquipamento.Anel2, 3),
            Item(PosicaoEquipamento.Arma1, 10),
            Item(PosicaoEquipamento.Arma2, 20),
            Item(PosicaoEquipamento.Inventario, 99),
        };
        Assert.Equal(15, Assert.Single(RegrasEquipamentos.Somar(itens, 1)).Valor);
        Assert.Equal(25, Assert.Single(RegrasEquipamentos.Somar(itens, 2)).Valor);
        Assert.Equal(15, Assert.Single(RegrasEquipamentos.Somar(itens, 1)).Valor);
    }
}
