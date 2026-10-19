using JogoDoGrupo.Shared.Features.Mobs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasBestiarioTestes
{
    [Theory]
    [InlineData(0, NivelDeConhecimentoBestiario.Desconhecido)]
    [InlineData(-5, NivelDeConhecimentoBestiario.Desconhecido)]
    [InlineData(1, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(4, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(5, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(19, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(20, NivelDeConhecimentoBestiario.Mestre)]
    [InlineData(100, NivelDeConhecimentoBestiario.Mestre)]
    public void CriaturaComum_CalculaNivelCorretamente(
        int derrotas,
        NivelDeConhecimentoBestiario esperado
    )
    {
        var nivel = RegrasBestiario.CalcularNivel(CategoriaDeCriatura.Comum, derrotas);
        Assert.Equal(esperado, nivel);
    }

    [Theory]
    [InlineData(CategoriaDeCriatura.Chefe, 0, NivelDeConhecimentoBestiario.Desconhecido)]
    [InlineData(CategoriaDeCriatura.Chefe, 1, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(CategoriaDeCriatura.Chefe, 2, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(CategoriaDeCriatura.Chefe, 3, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(CategoriaDeCriatura.Chefe, 4, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(CategoriaDeCriatura.Chefe, 5, NivelDeConhecimentoBestiario.Mestre)]
    [InlineData(CategoriaDeCriatura.ChefeRaro, 1, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(CategoriaDeCriatura.ChefeRaro, 3, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(CategoriaDeCriatura.ChefeRaro, 5, NivelDeConhecimentoBestiario.Mestre)]
    [InlineData(CategoriaDeCriatura.ChefeDeMundo, 1, NivelDeConhecimentoBestiario.Avistado)]
    [InlineData(CategoriaDeCriatura.ChefeDeMundo, 3, NivelDeConhecimentoBestiario.Estudado)]
    [InlineData(CategoriaDeCriatura.ChefeDeMundo, 5, NivelDeConhecimentoBestiario.Mestre)]
    public void CriaturaEspecial_CalculaNivelComThresholdsReduzidos(
        CategoriaDeCriatura categoria,
        int derrotas,
        NivelDeConhecimentoBestiario esperado
    )
    {
        var nivel = RegrasBestiario.CalcularNivel(categoria, derrotas);
        Assert.Equal(esperado, nivel);
    }

    [Fact]
    public void DerrotasParaProximoNivel_AvancaConformeNivelAtual()
    {
        Assert.Equal(1, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Comum, 0));
        Assert.Equal(5, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Comum, 1));
        Assert.Equal(20, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Comum, 5));
        Assert.Equal(20, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Comum, 20));

        Assert.Equal(1, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Chefe, 0));
        Assert.Equal(3, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Chefe, 1));
        Assert.Equal(5, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Chefe, 3));
        Assert.Equal(5, RegrasBestiario.DerrotasParaProximoNivel(CategoriaDeCriatura.Chefe, 5));
    }

    [Fact]
    public void ObterProgressoNoNivel_CalculaFracoesParaUI()
    {
        // 0 derrotas: no nível Desconhecido (0), meta para Avistado (1)
        RegrasBestiario.ObterProgressoNoNivel(
            CategoriaDeCriatura.Comum,
            0,
            out int progresso,
            out int meta
        );
        Assert.Equal(0, progresso);
        Assert.Equal(1, meta);

        // 3 derrotas: no nível Avistado (1), meta para Estudado (5) -> piso 1, teto 5 -> progresso 2 de 4
        RegrasBestiario.ObterProgressoNoNivel(
            CategoriaDeCriatura.Comum,
            3,
            out progresso,
            out meta
        );
        Assert.Equal(2, progresso);
        Assert.Equal(4, meta);

        // 10 derrotas: no nível Estudado (5), meta para Mestre (20) -> piso 5, teto 20 -> progresso 5 de 15
        RegrasBestiario.ObterProgressoNoNivel(
            CategoriaDeCriatura.Comum,
            10,
            out progresso,
            out meta
        );
        Assert.Equal(5, progresso);
        Assert.Equal(15, meta);

        // 25 derrotas: Mestre atingido
        RegrasBestiario.ObterProgressoNoNivel(
            CategoriaDeCriatura.Comum,
            25,
            out progresso,
            out meta
        );
        Assert.Equal(20, progresso);
        Assert.Equal(20, meta);
    }

    [Fact]
    public void CatalogoBestiario_TodasAsCriaturasDoCatalogoPossuemFichaCompleta()
    {
        foreach (var criatura in CatalogoCriaturas.Todas)
        {
            var ficha = CatalogoBestiario.Obter(criatura.Id);
            Assert.NotNull(ficha);
            Assert.Equal(criatura.Id, ficha.CriaturaId);
            Assert.False(string.IsNullOrWhiteSpace(ficha.Regiao));
            Assert.False(string.IsNullOrWhiteSpace(ficha.DescricaoGeral));
            Assert.False(string.IsNullOrWhiteSpace(ficha.SinaisDeAtaque));
            Assert.False(string.IsNullOrWhiteSpace(ficha.Fraquezas));
            Assert.False(string.IsNullOrWhiteSpace(ficha.DropsConhecidos));
            Assert.False(string.IsNullOrWhiteSpace(ficha.FraseBestiario));
        }
    }
}
