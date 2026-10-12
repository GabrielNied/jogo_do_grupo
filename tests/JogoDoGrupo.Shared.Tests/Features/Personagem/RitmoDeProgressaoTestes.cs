using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

/// <summary>Simulação do ritmo: mobs por minuto x XP por mob x curva, contra a meta da doc.</summary>
public class RitmoDeProgressaoTestes
{
    private static double MinutosPara(int nivel) =>
        RegrasExperiencia.XpParaOProximoNivel(nivel)
        / (ConfigExperiencia.MobsPorMinutoDeReferencia * ConfigExperiencia.XpPorMobDeReferencia);

    [Fact]
    public void Mob_de_referencia_e_o_comum_do_catalogo() =>
        Assert.Equal(
            ConfigExperiencia.XpPorMobDeReferencia,
            CatalogoCriaturas
                .Todas.First(c => c.Id == CatalogoCriaturas.JavaliDoCampo)
                .ExperienciaBase
        );

    [Fact]
    public void Chegar_ao_nivel_da_meta_leva_o_tempo_previsto()
    {
        double total = 0;
        for (var n = 1; n < ConfigExperiencia.NivelDaMetaDeRitmo; n++)
            total += MinutosPara(n);

        Assert.InRange(
            total,
            ConfigExperiencia.MinutosMinimosAteNivelDaMeta,
            ConfigExperiencia.MinutosMaximosAteNivelDaMeta
        );
    }

    [Fact]
    public void Cada_nivel_inicial_demora_mais_que_o_anterior_e_cabe_no_limite()
    {
        double anterior = 0;
        for (var n = 1; n < ConfigExperiencia.NivelDaMetaDeRitmo; n++)
        {
            var minutos = MinutosPara(n);
            Assert.True(minutos > anterior, $"nível {n} não demora mais que o anterior");
            Assert.True(minutos <= ConfigExperiencia.MinutosMaximosPorNivelInicial);
            anterior = minutos;
        }
    }

    [Fact]
    public void Primeiro_nivel_sai_com_poucos_mobs() =>
        Assert.True(MinutosPara(1) <= 1.5, "o primeiro nível deve sair em até 1,5 minuto");

    [Fact]
    public void Simulacao_mob_a_mob_chega_ao_nivel_da_meta_dentro_da_faixa()
    {
        var nivel = 1;
        long xp = 0;
        var mobs = 0;
        while (nivel < ConfigExperiencia.NivelDaMetaDeRitmo)
        {
            var r = RegrasExperiencia.AplicarGanho(
                nivel,
                xp,
                ConfigExperiencia.XpPorMobDeReferencia
            );
            nivel = r.Nivel;
            xp = r.Xp;
            mobs++;
        }

        var minutos = mobs / ConfigExperiencia.MobsPorMinutoDeReferencia;
        Assert.InRange(
            minutos,
            ConfigExperiencia.MinutosMinimosAteNivelDaMeta - 1,
            ConfigExperiencia.MinutosMaximosAteNivelDaMeta
        );
    }

    [Fact]
    public void Nivel_concede_pontos_de_atributo_por_nivel_subido()
    {
        var r = RegrasExperiencia.AplicarGanho(1, 0, long.MaxValue / 4);
        Assert.Equal(ConfigExperiencia.NivelMaximo, r.Nivel);
        Assert.Equal(
            (ConfigExperiencia.NivelMaximo - 1) * ConfigExperiencia.PontosPorNivel,
            r.PontosGanhos
        );
    }
}
