using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Missoes;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Missoes;

/// <summary>
/// Testes das regras de reset diário e semanal no horário de Brasília e rotação determinística (#1062).
/// </summary>
public sealed class RegrasCicloResetTestes
{
    [Fact]
    public void ResetDiario_ViraExatamenteMeiaNoiteDeBrasilia()
    {
        // 2026-10-20 às 23:59:50 no horário de Brasília (-03:00)
        // Em UTC, isso equivale a 2026-10-21T02:59:50Z
        var antesDoReset = new DateTimeOffset(
            2026,
            10,
            20,
            23,
            59,
            50,
            ConfigMissoes.FusoHorarioBrasilia
        );

        // 2026-10-21 às 00:00:10 no horário de Brasília (-03:00)
        // Em UTC, isso equivale a 2026-10-21T03:00:10Z
        var depoisDoReset = new DateTimeOffset(
            2026,
            10,
            21,
            0,
            0,
            10,
            ConfigMissoes.FusoHorarioBrasilia
        );

        Assert.Equal(20261020, RegrasCicloReset.ObterSeedDiario(antesDoReset));
        Assert.Equal(20261021, RegrasCicloReset.ObterSeedDiario(depoisDoReset));

        // Mesmos segundos antes do reset
        Assert.True(
            RegrasCicloReset.MesmoCicloDiario(
                antesDoReset.ToUnixTimeSeconds(),
                antesDoReset.AddSeconds(-3600).ToUnixTimeSeconds()
            )
        );

        // Antes vs depois: ciclo diferente
        Assert.False(
            RegrasCicloReset.MesmoCicloDiario(
                antesDoReset.ToUnixTimeSeconds(),
                depoisDoReset.ToUnixTimeSeconds()
            )
        );
    }

    [Fact]
    public void ResetSemanal_ViraExatamenteTercaFeiraMeiaNoiteDeBrasilia()
    {
        // 2026-10-19 é Segunda-feira
        var segundaFeiraFim = new DateTimeOffset(
            2026,
            10,
            19,
            23,
            59,
            50,
            ConfigMissoes.FusoHorarioBrasilia
        );

        // 2026-10-20 é Terça-feira (dia de reset semanal)
        var tercaFeiraInicio = new DateTimeOffset(
            2026,
            10,
            20,
            0,
            0,
            10,
            ConfigMissoes.FusoHorarioBrasilia
        );

        // Segunda e Terça caem em semanas de reset diferentes
        Assert.False(
            RegrasCicloReset.MesmoCicloSemanal(
                segundaFeiraFim.ToUnixTimeSeconds(),
                tercaFeiraInicio.ToUnixTimeSeconds()
            )
        );

        // Terça-feira e Quinta-feira da mesma semana caem no mesmo ciclo semanal
        var quintaFeira = new DateTimeOffset(
            2026,
            10,
            22,
            15,
            30,
            0,
            ConfigMissoes.FusoHorarioBrasilia
        );
        Assert.True(
            RegrasCicloReset.MesmoCicloSemanal(
                tercaFeiraInicio.ToUnixTimeSeconds(),
                quintaFeira.ToUnixTimeSeconds()
            )
        );
    }

    [Fact]
    public void RotacaoDiaria_SorteioDeterministico_MesmoDiaRetornaMesmasMissoes()
    {
        var momento1 = new DateTimeOffset(
            2026,
            10,
            20,
            10,
            0,
            0,
            ConfigMissoes.FusoHorarioBrasilia
        );
        var momento2 = new DateTimeOffset(
            2026,
            10,
            20,
            22,
            0,
            0,
            ConfigMissoes.FusoHorarioBrasilia
        );

        var concluidas = new int[]
        {
            CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
            CatalogoDeMissoes.Missao5LimpandoATrilhaNorte,
            CatalogoDeMissoes.Missao6OEnxameNaMataFria,
            CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia,
        };

        var rot1 = RegrasCicloReset.ObterRotacaoDiaria(
            CatalogoDeMissoes.Todas,
            nivelPersonagem: 3,
            missoesConcluidas: concluidas,
            momentoUtc: momento1
        );

        var rot2 = RegrasCicloReset.ObterRotacaoDiaria(
            CatalogoDeMissoes.Todas,
            nivelPersonagem: 3,
            missoesConcluidas: concluidas,
            momentoUtc: momento2
        );

        Assert.Equal(ConfigMissoes.QuantidadeDiariasPorDia, rot1.Count);
        Assert.Equal(rot1.Count, rot2.Count);

        for (int i = 0; i < rot1.Count; i++)
        {
            Assert.Equal(rot1[i].Id, rot2[i].Id);
        }
    }

    [Fact]
    public void RotacaoDiaria_FiltraPorNivelEPreRequisitosDeRegiao()
    {
        var momento = new DateTimeOffset(2026, 10, 20, 12, 0, 0, ConfigMissoes.FusoHorarioBrasilia);

        // Personagem nível 1 com apenas Etapa 4 concluída
        var concluidasNv1 = new int[] { CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes };
        var rotNv1 = RegrasCicloReset.ObterRotacaoDiaria(
            CatalogoDeMissoes.Todas,
            nivelPersonagem: 1,
            missoesConcluidas: concluidasNv1,
            momentoUtc: momento
        );

        // Apenas a missão 202 (Provisões para o Caldeirão) pede nível 1 e Etapa 4
        Assert.Single(rotNv1);
        Assert.Equal(CatalogoDeMissoes.Missao202AbastecerATaverna, rotNv1[0].Id);

        // Ao subir de nível e concluir Etapa 5, a piscina aumenta
        var concluidasNv2 = new int[]
        {
            CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
            CatalogoDeMissoes.Missao5LimpandoATrilhaNorte,
        };
        var rotNv2 = RegrasCicloReset.ObterRotacaoDiaria(
            CatalogoDeMissoes.Todas,
            nivelPersonagem: 2,
            missoesConcluidas: concluidasNv2,
            momentoUtc: momento
        );

        Assert.Equal(ConfigMissoes.QuantidadeDiariasPorDia, rotNv2.Count);
        foreach (var m in rotNv2)
        {
            Assert.True(m.NivelMinimo <= 2);
        }
    }

    [Fact]
    public void RotacaoSemanal_RetornaMissaoSemanalValida()
    {
        var momento = new DateTimeOffset(2026, 10, 20, 12, 0, 0, ConfigMissoes.FusoHorarioBrasilia);
        var concluidas = new int[]
        {
            CatalogoDeMissoes.Missao5LimpandoATrilhaNorte,
            CatalogoDeMissoes.Missao6OEnxameNaMataFria,
            CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia,
        };

        var rotSemanal = RegrasCicloReset.ObterRotacaoSemanal(
            CatalogoDeMissoes.Todas,
            nivelPersonagem: 3,
            missoesConcluidas: concluidas,
            momentoUtc: momento
        );

        Assert.Single(rotSemanal);
        Assert.Equal(TipoDeMissao.Semanal, rotSemanal[0].Tipo);
    }
}
