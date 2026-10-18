using System.Linq;
using System.Reflection;
using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Shared.Tests.Core.Api;

public class CodigosErroTestes
{
    private static string[][] Codigos() =>
        typeof(CodigosErro)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => new[] { f.Name, (string)f.GetRawConstantValue()! })
            .ToArray();

    [Fact]
    public void Valores_sao_iguais_aos_nomes()
    {
        foreach (var c in Codigos())
            Assert.Equal(c[0], c[1]);
    }

    [Fact]
    public void Valores_sao_unicos()
    {
        var valores = Codigos().Select(c => c[1]).ToArray();
        Assert.Equal(valores.Length, valores.Distinct().Count());
    }

    [Fact]
    public void Contem_os_codigos_genericos()
    {
        var nomes = Codigos().Select(c => c[0]).ToArray();
        Assert.Contains("RotaNaoEncontrada", nomes);
        Assert.Contains("MetodoNaoPermitido", nomes);
        Assert.Contains("AcessoNegado", nomes);
        Assert.Contains("ServidorOcupado", nomes);
        Assert.Contains("MuitasTentativas", nomes);
    }
}
