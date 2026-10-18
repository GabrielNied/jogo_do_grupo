using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class RegrasBaloesDeFalaTestes
{
    [Theory]
    [InlineData(CanalDeChat.Cidade, true)]
    [InlineData(CanalDeChat.Grupo, true)]
    [InlineData(CanalDeChat.Sussurro, false)]
    [InlineData((CanalDeChat)99, false)]
    public void CanalPermiteBalao_ApenasCanaisPublicosPermitidos(CanalDeChat canal, bool esperado)
    {
        Assert.Equal(esperado, RegrasBaloesDeFala.CanalPermiteBalao(canal));
    }

    [Fact]
    public void CalcularDuracao_TextoNuloOuVazio_RetornaDuracaoMinima()
    {
        Assert.Equal(
            RegrasBaloesDeFala.DuracaoMinimaSegundos,
            RegrasBaloesDeFala.CalcularDuracao(null)
        );
        Assert.Equal(
            RegrasBaloesDeFala.DuracaoMinimaSegundos,
            RegrasBaloesDeFala.CalcularDuracao("")
        );
        Assert.Equal(
            RegrasBaloesDeFala.DuracaoMinimaSegundos,
            RegrasBaloesDeFala.CalcularDuracao("   ")
        );
    }

    [Fact]
    public void CalcularDuracao_TextoCurto_FicaNaDuracaoMinima()
    {
        // 5 caracteres: 3.5 + 0.25 = 3.75, travado no mínimo de 4.0
        float duracao = RegrasBaloesDeFala.CalcularDuracao("Olá!");
        Assert.Equal(RegrasBaloesDeFala.DuracaoMinimaSegundos, duracao);
    }

    [Fact]
    public void CalcularDuracao_TextoMedio_CalculaProporcional()
    {
        // 30 caracteres: 3.5 + 30 * 0.05 = 5.0s
        string texto = new string('a', 30);
        float duracao = RegrasBaloesDeFala.CalcularDuracao(texto);
        Assert.Equal(5.0f, duracao, precision: 2);
    }

    [Fact]
    public void CalcularDuracao_TextoLongo_TravaNoMaximo()
    {
        // 100 caracteres: 3.5 + 5.0 = 8.5s, travado no teto de 7.0s
        string textoLongo = new string('a', 100);
        float duracao = RegrasBaloesDeFala.CalcularDuracao(textoLongo);
        Assert.Equal(RegrasBaloesDeFala.DuracaoMaximaSegundos, duracao);
    }

    [Theory]
    [InlineData(CanalDeChat.Cidade, 10f, true, true)]
    [InlineData(CanalDeChat.Cidade, 25f, true, true)]
    [InlineData(CanalDeChat.Cidade, 25.1f, true, false)] // além da distância máxima
    [InlineData(CanalDeChat.Cidade, 10f, false, false)] // atrás da câmera
    [InlineData(CanalDeChat.Grupo, 5f, true, true)]
    [InlineData(CanalDeChat.Sussurro, 5f, true, false)] // sussurro nunca aparece
    [InlineData(CanalDeChat.Cidade, -1f, true, false)] // distância negativa inválida
    public void DeveEstarVisivel_AvaliaCondicoesDeVisualizacao(
        CanalDeChat canal,
        float distancia,
        bool aFrente,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasBaloesDeFala.DeveEstarVisivel(canal, distancia, aFrente));
    }
}
