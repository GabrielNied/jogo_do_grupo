using System.Net;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class RecuperacaoTestes(ServidorDeSecundarios servidor)
    : IClassFixture<ServidorDeSecundarios>
{
    private async Task<(HttpClient Cliente, long Id)> NovoPersonagem()
    {
        servidor.Relogio.Reiniciar();
        servidor.Arvore.Modificadores = [];
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (cliente, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    [Fact]
    public void Estado_de_combate_encerra_apos_5_segundos_sem_atividade()
    {
        var relogioMs = 10000L;
        var estado = new ServicoDeEstadoDeCombate(() => relogioMs);

        Assert.False(estado.EmCombate(1));
        Assert.Equal(0, estado.SegundosRestantes(1));

        estado.RegistrarAtividade(1);
        Assert.True(estado.EmCombate(1));
        Assert.Equal(5, estado.SegundosRestantes(1));

        relogioMs += 2000L;
        Assert.True(estado.EmCombate(1));
        Assert.Equal(3, estado.SegundosRestantes(1));

        relogioMs += 3001L;
        Assert.False(estado.EmCombate(1));
        Assert.Equal(0, estado.SegundosRestantes(1));
    }

    [Fact]
    public void Entrar_em_combate_cancela_descanso_automaticamente()
    {
        var cancelou = false;
        var estado = new ServicoDeEstadoDeCombate();
        estado.DefinirCallbackEntrada(id => cancelou = true);

        estado.RegistrarAtividade(1);
        Assert.True(cancelou);
    }

    [Fact]
    public async Task Fonte_de_recuperacao_devolve_modificador_de_refeicao_ativa()
    {
        var agoraMs = 50000L;
        var estado = new ServicoDeEstadoDeCombate(() => agoraMs);
        using var servico = new ServicoDeRecuperacao(
            null!,
            estado,
            () => null!,
            servidor.Falso.Services.GetRequiredService<IServiceScopeFactory>(),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<ServicoDeRecuperacao>(),
            relogioMs: () => agoraMs
        );

        // Sem refeição inicial
        var modsIniciais = await servico.ObterAsync(1, CancellationToken.None);
        Assert.Empty(modsIniciais);
    }

    [Fact]
    public async Task Consumo_de_itens_retira_da_bolsa_com_sucesso()
    {
        var (_, id) = await NovoPersonagem();
        using var db = servidor
            .Falso.Services.CreateScope()
            .ServiceProvider.GetRequiredService<JogoDbContext>();
        var consumo = new ConsumoDeItens(db);

        var item = new ItemInventario
        {
            PersonagemId = id,
            ItemId = CatalogoItens.PocaoDeTeste,
            Posicao = PosicaoEquipamento.Inventario,
            Conteiner = ConteinerItem.Bolsa,
        };
        db.Set<ItemInventario>().Add(item);
        await db.SaveChangesAsync();

        var consumido = await consumo.ConsumirConsumivelAsync(
            id,
            0,
            CatalogoItens.PocaoDeTeste,
            CancellationToken.None
        );
        Assert.Equal(CatalogoItens.PocaoDeTeste, consumido);

        var existeAinda = await db.Set<ItemInventario>().AnyAsync(i => i.Id == item.Id);
        Assert.False(existeAinda);
    }

    [Fact]
    public async Task Consumo_de_itens_recusa_item_favorito()
    {
        var (_, id) = await NovoPersonagem();
        using var db = servidor
            .Falso.Services.CreateScope()
            .ServiceProvider.GetRequiredService<JogoDbContext>();
        var consumo = new ConsumoDeItens(db);

        var item = new ItemInventario
        {
            PersonagemId = id,
            ItemId = CatalogoItens.PocaoDeTeste,
            Posicao = PosicaoEquipamento.Inventario,
            Conteiner = ConteinerItem.Bolsa,
        };
        item.GravarEquipamento(new Equipamento { Nome = "Poção Favorita", Favorito = true });
        db.Set<ItemInventario>().Add(item);
        await db.SaveChangesAsync();

        var consumido = await consumo.ConsumirConsumivelAsync(
            id,
            item.Id,
            CatalogoItens.PocaoDeTeste,
            CancellationToken.None
        );
        Assert.Null(consumido);
    }

    [Fact]
    public async Task Descanso_fora_de_combate_aplica_modificadores_de_regeneracao()
    {
        var agoraMs = 100000L;
        var estado = new ServicoDeEstadoDeCombate(() => agoraMs);
        using var servico = new ServicoDeRecuperacao(
            null!,
            estado,
            () => null!,
            null!,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<ServicoDeRecuperacao>(),
            relogioMs: () => agoraMs
        );

        var sessao = new JogoDoGrupo.Server.Core.TempoReal.SessaoTempoReal(
            null!,
            100,
            100,
            TimeProvider.System
        )
        {
            PersonagemId = 1,
        };
        servico.AoDescansar(sessao, new IntencaoDeDescanso { Descansando = true });

        var mods = await servico.ObterAsync(1, CancellationToken.None);
        Assert.Equal(3, mods.Count);

        var modVida = mods.First(m => m.Atributo == AtributoSecundario.RegeneracaoVida);
        Assert.Equal(ConfigRecuperacao.RegeneracaoVidaForaDeCombate, modVida.Valor);
        Assert.Equal("Descanso", modVida.Origem);

        var modStamina = mods.First(m => m.Atributo == AtributoSecundario.RegeneracaoStamina);
        Assert.Equal(ConfigRecuperacao.MultiplicadorDescanso - 1.0, modStamina.Valor);

        var modMana = mods.First(m => m.Atributo == AtributoSecundario.RegeneracaoMana);
        Assert.Equal(ConfigRecuperacao.MultiplicadorDescanso - 1.0, modMana.Valor);
    }
}
