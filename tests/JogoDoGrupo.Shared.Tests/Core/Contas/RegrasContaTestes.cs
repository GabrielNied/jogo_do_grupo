using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;

namespace JogoDoGrupo.Shared.Tests.Core.Contas;

public class RegrasContaTestes
{
    [Theory]
    [InlineData("Ana")]
    [InlineData("jogador_123")]
    [InlineData("abcdefghijklmnopqrst")] // 20 caracteres, o máximo
    public void ValidarUsuario_aceita_formato_valido(string usuario) =>
        Assert.Null(RegrasConta.ValidarUsuario(usuario));

    [Theory]
    [InlineData(null)]
    [InlineData("ab")] // menos de 3
    [InlineData("abcdefghijklmnopqrstu")] // 21 caracteres
    [InlineData("a!b")]
    [InlineData("João")]
    [InlineData("ana maria")]
    public void ValidarUsuario_recusa_formato_invalido(string? usuario) =>
        Assert.Equal(CodigosErro.UsuarioInvalido, RegrasConta.ValidarUsuario(usuario));

    [Theory]
    [InlineData(RegrasConta.SenhaMinimo)]
    [InlineData(RegrasConta.SenhaMaximo)]
    public void ValidarSenha_aceita_os_limites(int tamanho) =>
        Assert.Null(RegrasConta.ValidarSenha(new string('a', tamanho)));

    [Theory]
    [InlineData(null)]
    [InlineData(RegrasConta.SenhaMinimo - 1)]
    [InlineData(RegrasConta.SenhaMaximo + 1)]
    public void ValidarSenha_recusa_fora_dos_limites(int? tamanho) =>
        Assert.Equal(
            CodigosErro.SenhaInvalida,
            RegrasConta.ValidarSenha(tamanho is null ? null : new string('a', tamanho.Value))
        );

    [Theory]
    [InlineData("ana@exemplo.com")]
    [InlineData("Ana.Maria+jogo@sub.exemplo.com.br")]
    public void ValidarEmail_aceita_formato_valido(string email) =>
        Assert.Null(RegrasConta.ValidarEmail(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba.com")]
    [InlineData("@exemplo.com")]
    [InlineData("ana@@exemplo.com")]
    [InlineData("ana@exemplo")]
    [InlineData("ana@.com")]
    [InlineData("ana@exemplo.")]
    [InlineData("ana maria@exemplo.com")]
    public void ValidarEmail_recusa_formato_invalido(string? email) =>
        Assert.Equal(CodigosErro.EmailInvalido, RegrasConta.ValidarEmail(email));

    [Fact]
    public void ValidarEmail_recusa_acima_do_maximo() =>
        Assert.Equal(
            CodigosErro.EmailInvalido,
            RegrasConta.ValidarEmail(new string('a', RegrasConta.EmailMaximo) + "@a.com")
        );

    [Theory]
    [InlineData("000000", true)]
    [InlineData("123456", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12a456", false)]
    [InlineData(null, false)]
    public void CodigoRecuperacaoNoFormato_exige_seis_digitos(string? codigo, bool esperado) =>
        Assert.Equal(esperado, RegrasConta.CodigoRecuperacaoNoFormato(codigo));
}
