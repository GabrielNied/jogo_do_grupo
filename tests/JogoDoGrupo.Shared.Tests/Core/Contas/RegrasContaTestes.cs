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
}
