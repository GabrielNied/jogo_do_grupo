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

    [Fact]
    public void ValidarAparencia_nula_e_valida_e_assume_padrao()
    {
        Assert.Null(RegrasPersonagem.ValidarAparencia(null));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 3, 4)]
    public void ValidarAparencia_limites_validos(int tom, int cabelo, int cor)
    {
        var aparencia = new AparenciaPersonagem
        {
            TomDePele = tom,
            Cabelo = cabelo,
            CorDeCabelo = cor,
        };
        Assert.Null(RegrasPersonagem.ValidarAparencia(aparencia));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(4, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 4, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 5)]
    public void ValidarAparencia_fora_dos_limites_recusa(int tom, int cabelo, int cor)
    {
        var aparencia = new AparenciaPersonagem
        {
            TomDePele = tom,
            Cabelo = cabelo,
            CorDeCabelo = cor,
        };
        Assert.Equal(
            CodigosErroPersonagem.AparenciaInvalida,
            RegrasPersonagem.ValidarAparencia(aparencia)
        );
    }

    [Theory]
    [InlineData("Guerreiro", "guerreiro", true)]
    [InlineData("Guerreiro", "GUERREIRO", true)]
    [InlineData("Guerreiro", "  Guerreiro  ", true)]
    [InlineData("Guerreiro", "OutroNome", false)]
    [InlineData("Guerreiro", "", false)]
    [InlineData("Guerreiro", null, false)]
    public void ConfirmacaoDeNomeConfere_compara_corretamente(
        string real,
        string? confirmacao,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasPersonagem.ConfirmacaoDeNomeConfere(real, confirmacao));
    }
}
