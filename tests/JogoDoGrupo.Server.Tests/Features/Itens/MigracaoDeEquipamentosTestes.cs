using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>
/// Exemplares gravados antes da #615 (valores fracionários na escala do catálogo, sem
/// <c>TierOrigem</c> nem <c>VersaoDosAtributos</c>) abrem, equipam e aparecem com tier.
/// </summary>
public class MigracaoDeEquipamentosTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    // JSON como o servidor gravava antes: sem os campos novos. Status: 3 Vitalidade, 8 Defesa,
    // 0 Força, 4 Espírito. Peitoral do Vigia A (modelo 102) do grau pedido.
    private static string JsonLegado(int grau, int fator, int requisito, int infusoes = 0) =>
        $$"""
            {"ModeloId":102,"Nome":"Peitoral do Vigia","Regiao":"Vale","Familia":"Vigia/Bastião","Variacao":"A","Tipo":1,"Raridade":3,"Grau":{{grau}},"Nivel":1,"NivelPersonagemMinimo":{{requisito}},"DuasMaos":false,"Atributos":[{"Status":3,"Valor":{{6
                * fator}},"Fixo":true},{"Status":8,"Valor":{{30
                * fator}},"Fixo":true},{"Status":0,"Valor":{{6
                * fator}},"Fixo":false},{"Status":4,"Valor":{{3
                * fator}},"Fixo":false}],"IndiceInfusivel":0,"InfusoesUsadas":{{infusoes}},"ValorBaseInfusivel":{{6
                * fator}},"Favorito":false}
            """;

    private async Task<(HttpClient Cliente, long Id)> Personagem()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (cliente, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private async Task<long> Legado(long personagemId, string json, int casa)
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario
        {
            PersonagemId = personagemId,
            ItemId = 102,
            Aba = 0,
            Casa = casa,
            EquipamentoJson = json,
        };
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static async Task<EquipamentosResponse> Ler(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(
            await cliente.GetAsync(RegrasEquipamentos.RotaDe(id))
        );

    private async Task<string?> JsonGravado(long instancia)
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        return await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i => i.Id == instancia)
            .Select(i => i.EquipamentoJson)
            .SingleAsync();
    }

    [Fact]
    public async Task Exemplar_legado_abre_com_valores_de_0_a_100_e_tier_calculado()
    {
        var (cliente, id) = await Personagem();
        var inicial = await Legado(id, JsonLegado(0, 1, 1), 0);
        var forte = await Legado(id, JsonLegado(2, 4, 20), 1);

        var estado = await Ler(cliente, id);

        var pecaInicial = estado.Inventario.Single(e => e.InstanciaId == inicial).Equipamento;
        Assert.Equal(new double[] { 20, 16, 20, 20 }, pecaInicial.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.D, pecaInicial.TierOrigem);
        Assert.Equal(TierItem.D, RegrasTier.TierDe(pecaInicial));
        Assert.Equal(RegrasTier.VersaoAtual, pecaInicial.VersaoDosAtributos);
        var pecaForte = estado.Inventario.Single(e => e.InstanciaId == forte).Equipamento;
        Assert.Equal(new double[] { 80, 63, 80, 80 }, pecaForte.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.A, pecaForte.TierOrigem);
        Assert.Equal(GrauEquipamento.Forte, pecaForte.Grau);
    }

    [Fact]
    public async Task Exemplar_legado_equipa_com_os_atributos_migrados_e_sem_somar_no_personagem()
    {
        var (cliente, id) = await Personagem();
        var peca = await Legado(id, JsonLegado(0, 1, 1), 0);

        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/equipar",
            new PedidoEquipar { InstanciaId = peca, Posicao = PosicaoEquipamento.Peito }
        );

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        // Vitalidade 20 vira vida do item: 50 de vida máxima. Força 20 vira 3 de dano físico flat.
        Assert.Equal(50, estado.Atributos.VidaMaxima);
        Assert.Equal(3, estado.Atributos.BonusDanoFisico, 6);
        Assert.Equal(9.6, estado.Atributos.DefesaFisica, 6);
        Assert.Contains("\"VersaoDosAtributos\":3", await JsonGravado(peca));
    }

    [Fact]
    public async Task Exemplar_da_versao_1_troca_os_atributos_sem_mexer_no_tier_de_origem()
    {
        // Exemplar da #615 (escala de 0 a 100, versão 1) com os atributos do modelo anterior:
        // Destreza 40 e o Vigor-recurso 30 caem na mesma Agilidade e somam; Vitalidade vira Vigor.
        var (cliente, id) = await Personagem();
        var json =
            "{\"ModeloId\":102,\"Nome\":\"Peitoral\",\"Tipo\":1,\"Atributos\":["
            + "{\"Status\":1,\"Valor\":40,\"Fixo\":false},"
            + "{\"Status\":6,\"Valor\":30,\"Fixo\":false},"
            + "{\"Status\":3,\"Valor\":60,\"Fixo\":true}],"
            + "\"TierOrigem\":4,\"VersaoDosAtributos\":1,\"NivelFusao\":2,\"Favorito\":false}";
        var peca = await Legado(id, json, 0);

        var lida = (await Ler(cliente, id)).Inventario.Single(e => e.InstanciaId == peca);

        var atributos = lida.Equipamento.Atributos;
        Assert.Equal(
            new[] { StatusItem.RegeneracaoStamina, StatusItem.VidaMaxima },
            atributos.Select(a => a.Status)
        );
        Assert.Equal(new double[] { 70, 60 }, atributos.Select(a => a.Valor));
        Assert.Equal(RegrasTier.VersaoAtual, lida.Equipamento.VersaoDosAtributos);
        Assert.Equal(TierItem.S, lida.Equipamento.TierOrigem);
        // O tier atual é recalculado pelos valores novos: (70 + 60) / 2 = 65%, tier A.
        Assert.Equal(TierItem.A, RegrasTier.TierDe(lida.Equipamento));
        Assert.Equal(2, lida.Equipamento.NivelFusao);
    }

    [Fact]
    public async Task Migrador_grava_o_exemplar_convertido_uma_vez_e_fixa_o_tier_de_origem()
    {
        var (_, id) = await Personagem();
        var peca = await Legado(id, JsonLegado(2, 4, 20), 0);
        var migrador = new MigradorDeEquipamentos(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MigradorDeEquipamentos>.Instance
        );

        Assert.True(await migrador.MigrarAsync(default) >= 1);

        var json = await JsonGravado(peca);
        Assert.Contains("\"VersaoDosAtributos\":3", json);
        Assert.Contains($"\"TierOrigem\":{(int)TierItem.A}", json);
        Assert.Equal(0, await migrador.MigrarAsync(default));
        Assert.Equal(json, await JsonGravado(peca));
    }

    [Fact]
    public async Task Tier_de_origem_gravado_nao_acompanha_mudanca_nos_valores()
    {
        var (cliente, id) = await Personagem();
        var peca = await Legado(id, JsonLegado(2, 4, 20), 0);
        await new MigradorDeEquipamentos(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MigradorDeEquipamentos>.Instance
        ).MigrarAsync(default);

        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = await db.Set<ItemInventario>().SingleAsync(i => i.Id == peca);
            var dados = item.LerEquipamento()!;
            foreach (var atributo in dados.Atributos)
                atributo.Valor = ConfigTier.EscalaMaxima;
            item.GravarEquipamento(dados);
            await db.SaveChangesAsync();
        }

        var lida = (await Ler(cliente, id))
            .Inventario.Single(e => e.InstanciaId == peca)
            .Equipamento;
        Assert.Equal(TierItem.A, lida.TierOrigem);
        Assert.Equal(TierItem.SMais, RegrasTier.TierDe(lida));
    }

    [Fact]
    public async Task Infusoes_do_modelo_anterior_viram_nivel_de_fusao_ao_ler_e_ao_gravar()
    {
        var (cliente, id) = await Personagem();
        var peca = await Legado(id, JsonLegado(0, 1, 1, infusoes: 12), 0);

        var lida = (await Ler(cliente, id)).Inventario.Single(e => e.InstanciaId == peca);

        Assert.Equal(10, lida.Equipamento.NivelFusao);
        Assert.Equal(0, lida.Equipamento.InfusoesUsadas);
        var migrador = new MigradorDeEquipamentos(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MigradorDeEquipamentos>.Instance
        );
        await migrador.MigrarAsync(default);
        var json = await JsonGravado(peca);
        Assert.Contains("\"NivelFusao\":10", json);
        Assert.Contains("\"InfusoesUsadas\":0", json);
        Assert.DoesNotContain("Infusivel", json);
    }

    [Fact]
    public async Task Migrador_regrava_o_exemplar_de_antes_da_fusao_sem_mudar_o_que_vale()
    {
        // Exemplar da #615: escala nova, versão 1, mas sem o campo do nível de fusão.
        var (cliente, id) = await Personagem();
        var json =
            "{\"ModeloId\":102,\"Nome\":\"Peitoral\",\"Tipo\":1,\"Atributos\":[{\"Status\":0,\"Valor\":55,\"Fixo\":false}],"
            + "\"TierOrigem\":2,\"VersaoDosAtributos\":1,\"IndiceInfusivel\":0,\"InfusoesUsadas\":0,\"ValorBaseInfusivel\":55,\"Favorito\":false}";
        var peca = await Legado(id, json, 0);
        await new MigradorDeEquipamentos(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MigradorDeEquipamentos>.Instance
        ).MigrarAsync(default);

        var gravado = await JsonGravado(peca);

        Assert.Contains("\"NivelFusao\":0", gravado);
        var lida = (await Ler(cliente, id)).Inventario.Single(e => e.InstanciaId == peca);
        Assert.Equal(55, lida.Equipamento.Atributos[0].Valor);
        Assert.Equal(TierItem.B, lida.Equipamento.TierOrigem);
    }
}
