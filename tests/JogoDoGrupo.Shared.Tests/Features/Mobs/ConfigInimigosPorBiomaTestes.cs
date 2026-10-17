using System;
using System.Linq;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class ConfigInimigosPorBiomaTestes
{
    [Fact]
    public void Tabela_tem_entradas_para_todos_os_biomas_externos()
    {
        var biomasEsperados = new[]
        {
            Bioma.Campo,
            Bioma.Mata,
            Bioma.Colinas,
            Bioma.Deserto,
            Bioma.Neve,
            Bioma.Lava,
            Bioma.Pantano,
        };

        var biomasNaTabela = ConfigInimigosPorBioma.Tabela.Select(e => e.Bioma).Distinct();

        foreach (var bioma in biomasEsperados)
        {
            Assert.Contains(bioma, biomasNaTabela);
        }
    }

    [Fact]
    public void Entradas_tem_valores_validos_e_niveis_coerentes()
    {
        Assert.NotEmpty(ConfigInimigosPorBioma.Tabela);

        Assert.All(
            ConfigInimigosPorBioma.Tabela,
            entrada =>
            {
                Assert.True(entrada.CriaturaId > 0, "CriaturaId deve ser positivo");
                Assert.False(
                    string.IsNullOrWhiteSpace(entrada.NomeEspecie),
                    "NomeEspecie obrigatório"
                );
                Assert.InRange(entrada.NivelMinimo, 1, 50);
                Assert.InRange(entrada.NivelMaximo, entrada.NivelMinimo, 50);
                Assert.True(Enum.IsDefined(typeof(PeriodoDeAparicao), entrada.Periodo));
            }
        );
    }

    [Fact]
    public void Ha_especies_exclusivas_da_noite_e_do_dia()
    {
        var entradasNoturnas = ConfigInimigosPorBioma
            .Tabela.Where(e => e.Periodo == PeriodoDeAparicao.Noite)
            .ToArray();
        var entradasDiurnas = ConfigInimigosPorBioma
            .Tabela.Where(e => e.Periodo == PeriodoDeAparicao.Dia)
            .ToArray();
        var entradasSempre = ConfigInimigosPorBioma
            .Tabela.Where(e => e.Periodo == PeriodoDeAparicao.Sempre)
            .ToArray();

        Assert.NotEmpty(entradasNoturnas);
        Assert.NotEmpty(entradasDiurnas);
        Assert.NotEmpty(entradasSempre);

        // Confere que há espécies só da noite em múltiplos biomas
        var biomasNoturnos = entradasNoturnas.Select(e => e.Bioma).Distinct();
        Assert.Contains(Bioma.Campo, biomasNoturnos);
        Assert.Contains(Bioma.Mata, biomasNoturnos);
    }
}
