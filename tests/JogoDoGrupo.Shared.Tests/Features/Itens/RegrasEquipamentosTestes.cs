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
        Assert.Equal(skills, RegrasEquipamentos.SlotsSkills(raridade));
    }
}
