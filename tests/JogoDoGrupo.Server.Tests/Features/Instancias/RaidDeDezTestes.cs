using JogoDoGrupo.Shared.Features.Instancias;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

public sealed class RaidDeDezTestes
{
    [Fact]
    public async Task Grupo_de_dez_entra_como_raid_com_o_lider_e_todos_na_mesma_area()
    {
        using var f = new InstanciasFixture();
        var jogadores = new List<InstanciasFixture.Jogador>();
        try
        {
            for (var i = 0; i < RegrasRaid.TamanhoMaximo; i++)
            {
                jogadores.Add(await f.EntrarAsync());
            }

            var ids = jogadores.Select(j => j.PersonagemId).ToArray();
            f.Grupos.Definir(300, ids[0], ids);

            jogadores[1]
                .Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
            var recusa = await jogadores[1]
                .Cliente.ReceberAsync<EntradaRecusada>(InstanciasMessageIds.EntradaRecusada);
            Assert.Equal(CodigosErroInstancias.NaoELider, recusa.Codigo);

            jogadores[0]
                .Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
            var inicios = new List<DungeonIniciada>();
            foreach (var j in jogadores)
            {
                inicios.Add(
                    await j.Cliente.ReceberAsync<DungeonIniciada>(
                        InstanciasMessageIds.DungeonIniciada
                    )
                );
            }

            Assert.All(inicios, i => Assert.True(i.Raid));
            Assert.Single(inicios.Select(i => i.IdInstancia).Distinct());
        }
        finally
        {
            foreach (var j in jogadores)
            {
                j.Dispose();
            }
        }
    }

    [Fact]
    public async Task Grupo_pequeno_continua_dungeon_sem_marca_de_raid()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();
        f.Grupos.Definir(301, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);

        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await lider.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        Assert.False(inicio.Raid);
    }

    [Fact]
    public async Task Grupo_acima_de_dez_e_recusado()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        var ids = new List<long> { lider.PersonagemId };
        for (var i = 0; i < RegrasRaid.TamanhoMaximo; i++)
        {
            ids.Add(1_000_000 + i);
        }

        f.Grupos.Definir(302, lider.PersonagemId, ids.ToArray());
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var recusa = await lider.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.GrupoGrandeDemais, recusa.Codigo);
    }
}
