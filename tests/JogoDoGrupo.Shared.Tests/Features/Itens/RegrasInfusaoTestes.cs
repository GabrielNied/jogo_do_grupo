using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasInfusaoTestes
{
    [Fact]
    public void DezInfusoesDeBase20ResultamEm24SemJurosCompostos()
    {
        var valor = 20d;
        for (var i = 0; i < 10; i++)
            valor = RegrasInfusao.Depois(valor, 20);
        Assert.Equal(24, valor);
        Assert.Equal(20.4, RegrasInfusao.Depois(20, 20));
    }

    [Theory]
    [InlineData(RaridadeItem.Comum, 10)]
    [InlineData(RaridadeItem.Incomum, 15)]
    [InlineData(RaridadeItem.Raro, 20)]
    [InlineData(RaridadeItem.Epico, 25)]
    [InlineData(RaridadeItem.Lendario, 30)]
    public void UltimoSlotAceitoETetoRecusado(RaridadeItem raridade, int limite)
    {
        var receptor = CatalogoEquipamentos.Obter(100);
        var doador = CatalogoEquipamentos.Obter(100);
        receptor.Raridade = doador.Raridade = raridade;
        receptor.InfusoesUsadas = limite - 1;
        Assert.Null(RegrasInfusao.Validar(receptor, doador));
        receptor.InfusoesUsadas++;
        Assert.Equal("LimiteInfusoes", RegrasInfusao.Validar(receptor, doador));
    }

    [Fact]
    public void RegiaoEGrauDiferentesAceitosMasTipoRaridadeEFavoritoProtegidos()
    {
        var receptor = CatalogoEquipamentos.Obter(101);
        var doador = CatalogoEquipamentos.Obter(110);
        doador.Regiao = "Serra";
        doador.Grau = GrauEquipamento.Forte;
        Assert.Null(RegrasInfusao.Validar(receptor, doador));
        doador.Favorito = true;
        Assert.Equal("ItemFavorito", RegrasInfusao.Validar(receptor, doador));
        doador.Favorito = false;
        doador.Raridade = RaridadeItem.Raro;
        Assert.Equal("DoadorIncompativel", RegrasInfusao.Validar(receptor, doador));
        doador.Raridade = receptor.Raridade;
        doador.Tipo = TipoEquipamento.Botas;
        Assert.Equal("DoadorIncompativel", RegrasInfusao.Validar(receptor, doador));
    }
}
