using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>Os bosses por área (o contrato para a instância) e o boss de teste no servidor.</summary>
public class ChefesDaDungeonTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public void Criar_poe_um_boss_por_area_com_ids_distintos()
    {
        var chefes = new ChefesDaDungeon();

        var a = chefes.Criar("dungeon-a", 0, 0);
        var b = chefes.Criar("dungeon-b", 5, 5);

        Assert.NotEqual(a, b);
        Assert.True(a >= ConfigChefeDaDungeon.PrimeiroIdDoChefe);
        Assert.Equal(a, chefes.Obter("dungeon-a")!.InimigoId);
        Assert.Equal(b, chefes.Obter("dungeon-b")!.InimigoId);
        Assert.Equal(2, chefes.Ativos().Count);
        Assert.Null(chefes.Obter("outra"));
    }

    [Fact]
    public void Criar_de_novo_na_mesma_area_troca_o_boss()
    {
        var chefes = new ChefesDaDungeon();
        var primeiro = chefes.Criar("dungeon-a", 0, 0);
        chefes.Obter("dungeon-a")!.TentarAplicarDano(primeiro, 100, 1, out _);

        var segundo = chefes.Criar("dungeon-a", 0, 0);

        Assert.NotEqual(primeiro, segundo);
        Assert.Single(chefes.Ativos());
        var retrato = Assert.Single(chefes.Obter("dungeon-a")!.Vivos());
        Assert.Equal(retrato.VidaMaxima, retrato.Vida);
        Assert.False(chefes.TentarAplicarDano(primeiro, 10, 1, out _));
    }

    [Fact]
    public void Remover_tira_o_boss_da_area()
    {
        var chefes = new ChefesDaDungeon();
        var id = chefes.Criar("dungeon-a", 0, 0);

        Assert.True(chefes.Remover("dungeon-a"));
        Assert.False(chefes.Remover("dungeon-a"));
        Assert.False(chefes.Remover(null!));
        Assert.Empty(chefes.Ativos());
        Assert.False(chefes.TentarAplicarDano(id, 10, 1, out _));
        Assert.Empty(chefes.AlvosNoVolume("dungeon-a", -2, 0, 90));
    }

    [Fact]
    public void Criar_recusa_area_vazia()
    {
        Assert.Throws<ArgumentException>(() => new ChefesDaDungeon().Criar("", 0, 0));
    }

    [Fact]
    public void Como_alvo_do_combate_acha_o_boss_pela_area_e_aplica_o_dano()
    {
        var chefes = new ChefesDaDungeon();
        var id = chefes.Criar("dungeon-a", 0, 0);
        chefes.Criar("dungeon-b", 100, 100);

        var alvo = Assert.Single(chefes.AlvosNoVolume("dungeon-a", -2, 0, 90));
        Assert.Equal(id, alvo.Id);
        Assert.Empty(chefes.AlvosNoVolume("cidade", -2, 0, 90));

        Assert.True(chefes.TentarAplicarDano(id, 50, 7, out var resultado));
        Assert.Equal(id, resultado.AlvoId);
        Assert.Equal(50, resultado.Dano);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima - 50, resultado.VidaRestante);
    }

    [Fact]
    public void O_servidor_sobe_com_o_boss_de_teste_fora_da_cidade()
    {
        var servicos = f.Fabrica.Services;
        var chefes = servicos.GetRequiredService<ChefesDaDungeon>();

        var teste = chefes.Obter(ConfigChefeDaDungeon.AreaDeTeste);

        Assert.NotNull(teste);
        var retrato = Assert.Single(teste.Vivos());
        Assert.Equal(ConfigChefeDaDungeon.PontoDeTesteX, retrato.X, 1);
        Assert.Equal(ConfigChefeDaDungeon.PontoDeTesteZ, retrato.Z, 1);
        Assert.True(RegrasInimigo.Distancia(0, 0, retrato.X, retrato.Z) > 100.0);
        Assert.Same(chefes, servicos.GetRequiredService<IChefesDaDungeon>());
        Assert.Contains(servicos.GetServices<IAlvosDeCombate>(), a => ReferenceEquals(a, chefes));
    }

    [Fact]
    public async Task Quem_entra_na_area_recebe_o_boss_junto_dos_inimigos_vivos()
    {
        var (cliente, _, personagemId) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);

        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        Assert.True(
            teleporte
                .Teleportar(
                    personagemId,
                    (float)ConfigChefeDaDungeon.PontoDeTesteX,
                    0f,
                    (float)ConfigChefeDaDungeon.PontoDeTesteZ
                )
                .Aceito
        );

        var servico = f.Fabrica.Services.GetRequiredService<ServicoDeInimigos>();
        servico.AoEntrarNaArea(
            new JogadorEntrouNaArea(personagemId, ConfigChefeDaDungeon.AreaDeTeste)
        );

        var area = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);

        var boss = Assert.Single(
            area.Inimigos,
            i => i.CriaturaId == ConfigChefeDaDungeon.CriaturaId
        );
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, boss.VidaMaxima);
        Assert.Equal(boss.VidaMaxima, boss.Vida);
        Assert.Equal(EstadoDeInimigo.Repouso, boss.Estado);
    }
}
