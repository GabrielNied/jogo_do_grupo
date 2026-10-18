using System.Linq;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class ContratoMarcadoresTestes
{
    [Fact]
    public void IdsDosMarcadores_EstaoDentroDaFaixaSocialEDistintos()
    {
        Assert.Equal(13320, SocialMessageIds.DefinirMarcadorDeAlvo);
        Assert.Equal(13321, SocialMessageIds.MarcadoresDeAlvoMudaram);
        Assert.Equal(13322, SocialMessageIds.DefinirMarcadorRecusado);

        Assert.True(SocialMessageIds.Faixa.Contem(SocialMessageIds.DefinirMarcadorDeAlvo));
        Assert.True(SocialMessageIds.Faixa.Contem(SocialMessageIds.MarcadoresDeAlvoMudaram));
        Assert.True(SocialMessageIds.Faixa.Contem(SocialMessageIds.DefinirMarcadorRecusado));
    }

    [Fact]
    public void TipoMarcadorDeAlvo_ContemValoresEsperados()
    {
        Assert.Equal(0, (byte)TipoMarcadorDeAlvo.Nenhum);
        Assert.Equal(1, (byte)TipoMarcadorDeAlvo.Caveira);
        Assert.Equal(2, (byte)TipoMarcadorDeAlvo.Cruz);
        Assert.Equal(3, (byte)TipoMarcadorDeAlvo.Escudo);
        Assert.Equal(4, (byte)TipoMarcadorDeAlvo.Lua);
        Assert.Equal(5, (byte)TipoMarcadorDeAlvo.Estrela);
    }

    [Fact]
    public void MarcadorAlvoDto_Instanciacao_PreservaValores()
    {
        var dto = new MarcadorAlvoDto(1234, TipoMarcadorDeAlvo.Caveira);
        Assert.Equal(1234, dto.AlvoId);
        Assert.Equal(TipoMarcadorDeAlvo.Caveira, dto.Marcador);
    }

    [Fact]
    public void MarcadoresDeAlvoMudaram_PadraoInicial_ColecaoNaoNula()
    {
        var msg = new MarcadoresDeAlvoMudaram();
        Assert.NotNull(msg.Marcadores);
        Assert.Empty(msg.Marcadores);
    }
}
