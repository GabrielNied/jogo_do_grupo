using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task Raid_avanca_em_tres_etapas_com_boss_e_concede_loot_semanal()
    {
        using var f = new InstanciasFixture();
        var jogadores = new List<InstanciasFixture.Jogador>();
        try
        {
            // Grupo de 6 membros = Raid
            for (var i = 0; i < 6; i++)
            {
                jogadores.Add(await f.EntrarAsync());
            }

            var ids = jogadores.Select(j => j.PersonagemId).ToArray();
            f.Grupos.Definir(305, ids[0], ids);

            // Líder inicia
            jogadores[0]
                .Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
            var inicio = await jogadores[0]
                .Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

            Assert.True(inicio.Raid);
            Assert.Equal(1, inicio.EtapaAtual);
            Assert.Equal(3, inicio.TotalEtapas);

            // Derrota Boss 1 (Presa-Velha, id 200)
            var derrota1 = new InimigoDerrotado(
                101,
                RegrasRaid.ChefeEtapa1CriaturaId,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                0,
                0,
                ids,
                1
            );
            f.Instancias.AoInimigoDerrotado(derrota1);

            var etapa1 = await jogadores[0]
                .Cliente.ReceberAsync<EtapaRaidConcluida>(InstanciasMessageIds.EtapaRaidConcluida);
            Assert.Equal(1, etapa1.EtapaConcluida);
            Assert.Equal(2, etapa1.ProximaEtapa);
            Assert.False(etapa1.EtapaFinal);
            Assert.True(etapa1.GanhouLootSemanal);

            // Derrota Boss 2 (Guardião do Reflexo, id 100)
            var derrota2 = new InimigoDerrotado(
                102,
                RegrasRaid.ChefeEtapa2CriaturaId,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                0,
                0,
                ids,
                2
            );
            f.Instancias.AoInimigoDerrotado(derrota2);

            var etapa2 = await jogadores[0]
                .Cliente.ReceberAsync<EtapaRaidConcluida>(InstanciasMessageIds.EtapaRaidConcluida);
            Assert.Equal(2, etapa2.EtapaConcluida);
            Assert.Equal(3, etapa2.ProximaEtapa);
            Assert.False(etapa2.EtapaFinal);
            Assert.True(etapa2.GanhouLootSemanal);

            // Derrota Boss 3 (Colosso Basáltico, id 301)
            var derrota3 = new InimigoDerrotado(
                103,
                RegrasRaid.ChefeEtapa3CriaturaId,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                0,
                0,
                ids,
                3
            );
            f.Instancias.AoInimigoDerrotado(derrota3);

            var etapa3 = await jogadores[0]
                .Cliente.ReceberAsync<EtapaRaidConcluida>(InstanciasMessageIds.EtapaRaidConcluida);
            Assert.Equal(3, etapa3.EtapaConcluida);
            Assert.True(etapa3.EtapaFinal);
            Assert.True(etapa3.GanhouLootSemanal);

            // Chefe final derrotado agenda vitória
            var chefeFinal = await jogadores[0]
                .Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
                    InstanciasMessageIds.ChefeDerrotadoNaDungeon
                );
            Assert.NotNull(chefeFinal);

            // Consulta bloqueio semanal via mensagem de rede
            jogadores[0]
                .Cliente.Enviar(
                    InstanciasMessageIds.ConsultarBloqueioSemanalRaid,
                    new ConsultarBloqueioSemanalRaid()
                );
            var respostaBloqueio = await jogadores[0]
                .Cliente.ReceberAsync<RespostaBloqueioSemanalRaid>(
                    InstanciasMessageIds.RespostaBloqueioSemanalRaid
                );
            Assert.Equal(new[] { 1, 2, 3 }, respostaBloqueio.EtapasConcluidas.OrderBy(x => x));

            // Verifica saldo de fusão creditado para os 3 chefes (3 materiais de raid)
            await using var escopo = f.Fabrica.Services.CreateAsyncScope();
            var materiais =
                escopo.ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Contratos.Itens.ISaldosMateriaisFusao>();
            var limite = DateTime.UtcNow.AddSeconds(5);
            var saldo = await materiais.SaldoAsync(jogadores[0].PersonagemId, default);
            while (saldo.Raid < 3 && DateTime.UtcNow < limite)
            {
                await Task.Delay(50);
                saldo = await materiais.SaldoAsync(jogadores[0].PersonagemId, default);
            }
            Assert.Equal(3, saldo.Raid);
        }
        finally
        {
            foreach (var j in jogadores)
            {
                j.Dispose();
            }
        }
    }
}
