using System;
using System.Linq;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class ConfigPerigosDoMundoTestes : IDisposable
{
    public ConfigPerigosDoMundoTestes()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = true;
    }

    public void Dispose()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = false;
    }

    [Fact]
    public void Constantes_de_perigos_possuem_valores_calibrados()
    {
        Assert.True(ConfigPerigosDoMundo.DanoPorSegundoLava > 0);
        Assert.Equal(1000, ConfigPerigosDoMundo.IntervaloDanoLavaMs);
        Assert.InRange(ConfigPerigosDoMundo.FatorVelocidadePantano, 0.1, 0.9);
        Assert.True(ConfigPerigosDoMundo.DuracaoTetoPantanoSegundos > 0);
    }

    [Fact]
    public void Cidade_e_campo_nao_sao_lava_nem_pantano()
    {
        Assert.False(ConfigPerigosDoMundo.EstaNaLava(0f, 0f));
        Assert.False(ConfigPerigosDoMundo.EstaNoPantano(0f, 0f));
        Assert.False(ConfigPerigosDoMundo.EstaNaLava(160f, -180f));
        Assert.False(ConfigPerigosDoMundo.EstaNoPantano(160f, -180f));
    }

    [Fact]
    public void Neve_e_deserto_nao_sao_lava_nem_pantano()
    {
        // Frio e calor não causam dano nem lentidão
        Assert.False(ConfigPerigosDoMundo.EstaNaLava(-920f, 1030f));
        Assert.False(ConfigPerigosDoMundo.EstaNoPantano(-920f, 1030f));
        Assert.False(ConfigPerigosDoMundo.EstaNaLava(-1080f, -680f));
        Assert.False(ConfigPerigosDoMundo.EstaNoPantano(-1080f, -680f));
    }

    [Fact]
    public void Mapa_possui_pontos_com_agua_de_pantano_e_rio_de_lava()
    {
        var achouPantano = false;
        var achouLava = false;

        // Amostra a região leste (pântano)
        for (var x = 800f; x <= 1300f && !achouPantano; x += 15f)
        {
            for (var z = 0f; z <= 500f && !achouPantano; z += 15f)
            {
                if (ConfigPerigosDoMundo.EstaNoPantano(x, z))
                {
                    achouPantano = true;
                }
            }
        }

        // Amostra a região sul (lava)
        for (var x = 500f; x <= 1200f && !achouLava; x += 15f)
        {
            for (var z = -1400f; z <= -800f && !achouLava; z += 15f)
            {
                if (ConfigPerigosDoMundo.EstaNaLava(x, z))
                {
                    achouLava = true;
                }
            }
        }

        Assert.True(achouPantano, "Deveria haver pontos de água do pântano no mapa.");
        Assert.True(achouLava, "Deveria haver pontos de rio de lava no mapa.");
    }
}
