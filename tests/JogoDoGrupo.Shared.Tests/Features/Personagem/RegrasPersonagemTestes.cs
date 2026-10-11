using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class RegrasPersonagemTestes
{
    [Theory]
    [InlineData("Ana")]
    [InlineData("ABCDEFGHIJKLMNOP")] // 16 letras, o máximo
    public void ValidarNome_aceita_so_letras_de_a_a_z(string nome) =>
        Assert.Null(RegrasPersonagem.ValidarNome(nome));

    [Theory]
    [InlineData(null)]
    [InlineData("An")]
    [InlineData("ABCDEFGHIJKLMNOPQ")] // 17 letras
    [InlineData("Ana Maria")]
    [InlineData("Joao1")]
    [InlineData("João")] // acento ainda não é aceito (questão em aberto)
    public void ValidarNome_recusa_formato_invalido(string? nome) =>
        Assert.Equal(CodigosErroPersonagem.NomeInvalido, RegrasPersonagem.ValidarNome(nome));
}
