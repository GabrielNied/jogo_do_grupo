using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>Sorteador de loot de mentira: todo participante recebe o que o teste mandar.</summary>
public sealed class SorteadorDeLootFalso : ISorteadorDeLoot
{
    public volatile int[] Resultado = [];

    /// <summary>Se definido, vale no lugar de <see cref="Resultado"/> (com raridade e grau).</summary>
    public volatile QuedaDeLoot[]? Quedas;

    public volatile OrigemDoLoot UltimaOrigem;
    public volatile int UltimoNivel;
    public volatile int UltimaCriaturaId;

    public QuedaDeLoot[] Sortear(OrigemDoLoot origem, int nivelDoInimigo, int criaturaId)
    {
        UltimaOrigem = origem;
        UltimoNivel = nivelDoInimigo;
        UltimaCriaturaId = criaturaId;
        return Quedas ?? Resultado.Select(id => new QuedaDeLoot { ModeloId = id }).ToArray();
    }
}

/// <summary>
/// Servidor de teste do loot: banco real, Mundo de verdade, sorteio e relógio controlados pelo
/// teste. A morte do inimigo é publicada direto (quem decide a morte é Mobs).
/// </summary>
public sealed class ServidorDeLoot : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public RelogioFalso Relogio { get; } = new();
    public SorteadorDeLootFalso Sorteador { get; } = new();
    public WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorDeLoot()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(Relogio);
                s.AddSingleton<ISorteadorDeLoot>(Sorteador);
            })
        );
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
    }

    public T Servico<T>()
        where T : notnull
    {
        return Falso.Services.GetRequiredService<T>();
    }

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

/// <summary>
/// Integração do loot no chão (fatia jogável, seção 5): client UDP de verdade, banco SQLite e o
/// inventário que já existe. O chão em si é testado sem rede em <see cref="ChaoDeLootTestes"/>.
/// </summary>
public class LootNoChaoTestes(ServidorDeLoot servidor) : IClassFixture<ServidorDeLoot>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Escudo = CatalogoItens.EscudoDeTeste;
    private const int Pocao = CatalogoItens.PocaoDeTeste;

    // Cada morte publicada tem a própria sequência, como as de Mobs.
    private static long _mortes;

    private async Task<Jogador> NovoJogadorAsync()
    {
        servidor.Relogio.Reiniciar();
        servidor.Sorteador.Quedas = null;
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);
        var cliente = await ConectarNaCidadeAsync(http, personagem.Id);
        return new Jogador(http, cliente, personagem.Id);
    }

    private async Task<ClienteTempoRealDeTeste> ConectarNaCidadeAsync(HttpClient http, long id)
    {
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            servidor.Porta,
            http.DefaultRequestHeaders.Authorization!.Parameter!,
            id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return cliente;
    }

    private PosicaoNoMundo PosicaoDe(Jogador j)
    {
        Assert.True(servidor.Servico<IPosicoesDoMundo>().TentarObter(j.Id, out var posicao));
        return posicao;
    }

    /// <summary>Publica a morte de um inimigo a <paramref name="distancia"/> metros do jogador.</summary>
    private void DerrotarInimigoPertoDe(Jogador perto, double distancia, params Jogador[] quem) =>
        DerrotarInimigoPertoDe(
            perto,
            distancia,
            RegrasInimigo.CriaturaDaFatia,
            VarianteDeCriatura.Normal,
            0,
            quem
        );

    private void DerrotarInimigoPertoDe(
        Jogador perto,
        double distancia,
        int criaturaId,
        VarianteDeCriatura variante,
        int nivel,
        params Jogador[] quem
    )
    {
        var posicao = PosicaoDe(perto);
        var derrota = new InimigoDerrotado(
            1,
            criaturaId,
            variante,
            40,
            1.0,
            posicao.Area,
            posicao.X + distancia,
            posicao.Z,
            quem.Select(j => j.Id).Order().ToArray(),
            Interlocked.Increment(ref _mortes),
            nivel
        );
        servidor.Servico<EventosDoServidor>().Publicar(derrota);
    }

    /// <summary>O jogador mata um inimigo aos seus pés e recebe os itens do sorteio.</summary>
    private async Task<LootNoChao[]> LargarParaAsync(Jogador j, params int[] itens)
    {
        servidor.Sorteador.Resultado = itens;
        DerrotarInimigoPertoDe(j, 0, j);
        var loot = new LootNoChao[itens.Length];
        for (var i = 0; i < loot.Length; i++)
        {
            loot[i] = (await ApareceuAsync(j)).Loot;
        }

        return loot;
    }

    private static Task<LootApareceu> ApareceuAsync(Jogador j) =>
        j.Cliente.ReceberAsync<LootApareceu>(ItensMessageIds.LootApareceu);

    private static Task<LootColetado> ColetadoAsync(Jogador j) =>
        j.Cliente.ReceberAsync<LootColetado>(ItensMessageIds.LootColetado);

    private static Task<ColetaRecusada> RecusadaAsync(Jogador j) =>
        j.Cliente.ReceberAsync<ColetaRecusada>(ItensMessageIds.ColetaRecusada);

    private static void Coletar(Jogador j, long lootId = RegrasLoot.TudoPorPerto) =>
        j.Cliente.Enviar(ItensMessageIds.ColetarLoot, new ColetarLoot { LootId = lootId });

    private static async Task<int[]> InventarioAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasItens.InventarioDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        var inventario = await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resposta);
        return inventario.Entradas.Select(e => e.ItemId).ToArray();
    }

    private async Task EncherInventarioAsync(Jogador j, int quantidade)
    {
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        for (var i = 0; i < quantidade; i++)
        {
            db.Add(new ItemInventario { PersonagemId = j.Id, ItemId = CatalogoItens.MoedaDeTeste });
        }

        await db.SaveChangesAsync();
    }

    private int NoChaoDe(Jogador j) =>
        servidor.Servico<ChaoDeLoot>().DoDono(j.Id, servidor.Relogio.GetUtcNow()).Count;

    [Fact]
    public async Task Participante_recebe_o_proprio_sorteio_onde_o_inimigo_caiu()
    {
        using var ana = await NovoJogadorAsync();
        var posicao = PosicaoDe(ana);

        var loot = await LargarParaAsync(ana, Espada, Pocao);

        Assert.Equal(new[] { Espada, Pocao }, loot.Select(l => l.ItemId));
        Assert.Equal(new[] { "Espada de teste", "Poção de teste" }, loot.Select(l => l.Nome));
        Assert.NotEqual(loot[0].LootId, loot[1].LootId);
        Assert.All(
            loot,
            l =>
            {
                Assert.NotEqual(RegrasLoot.TudoPorPerto, l.LootId);
                Assert.Equal(posicao.X, l.X, 3);
                Assert.Equal(posicao.Z, l.Z, 3);
                Assert.Equal(RegrasLoot.ReservaSegundos, l.ExpiraEmSegundos);
            }
        );
        Assert.Empty(await InventarioAsync(ana)); // Cair no chão não é ganhar.
    }

    [Fact]
    public async Task Cada_participante_tem_o_seu_e_quem_nao_participou_nao_recebe()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        using var caio = await NovoJogadorAsync();
        servidor.Sorteador.Resultado = [Escudo];

        DerrotarInimigoPertoDe(ana, 0, ana, beto);

        var daAna = (await ApareceuAsync(ana)).Loot;
        var doBeto = (await ApareceuAsync(beto)).Loot;
        Assert.NotEqual(daAna.LootId, doBeto.LootId);
        Assert.Equal(1, NoChaoDe(ana));
        Assert.Equal(1, NoChaoDe(beto));
        Assert.Equal(0, NoChaoDe(caio));
        Assert.False(caio.Recebeu(ItensMessageIds.LootApareceu));
    }

    [Fact]
    public async Task Sorteio_sem_itens_nao_poe_nada_no_chao()
    {
        using var ana = await NovoJogadorAsync();

        await LargarParaAsync(ana);
        Assert.Equal(0, NoChaoDe(ana));

        await LargarParaAsync(ana, Espada);
        Assert.Equal(1, ana.Recebidas(ItensMessageIds.LootApareceu));
    }

    [Fact]
    public async Task Coletar_um_item_grava_no_inventario_e_tira_do_chao()
    {
        using var ana = await NovoJogadorAsync();
        var loot = (await LargarParaAsync(ana, Pocao))[0];

        Coletar(ana, loot.LootId);

        var coletado = await ColetadoAsync(ana);
        Assert.Equal(loot.LootId, coletado.LootId);
        Assert.Equal(Pocao, coletado.Entrada.ItemId);
        Assert.Equal("Poção de teste", coletado.Entrada.Nome);
        Assert.Equal(new[] { Pocao }, await InventarioAsync(ana));
        Assert.Equal(0, NoChaoDe(ana));

        Coletar(ana, loot.LootId); // De novo: já não existe.
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(ana)).Codigo);
        Assert.Single(await InventarioAsync(ana));
    }

    [Fact]
    public async Task Tecla_F_coleta_tudo_o_que_e_do_jogador_por_perto()
    {
        using var ana = await NovoJogadorAsync();
        var loot = await LargarParaAsync(ana, Espada, Escudo);

        Coletar(ana);

        var primeiro = await ColetadoAsync(ana);
        var segundo = await ColetadoAsync(ana);
        Assert.Equal(loot.Select(l => l.LootId), new[] { primeiro.LootId, segundo.LootId });
        Assert.Equal(new[] { Espada, Escudo }, await InventarioAsync(ana));
        Assert.Equal(0, NoChaoDe(ana));
        Assert.False(ana.Recebeu(ItensMessageIds.ColetaRecusada));
    }

    [Fact]
    public async Task Tecla_F_sem_nada_por_perto_e_recusada()
    {
        using var ana = await NovoJogadorAsync();

        Coletar(ana);

        var recusa = await RecusadaAsync(ana);
        Assert.Equal(RegrasLoot.TudoPorPerto, recusa.LootId);
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, recusa.Codigo);
    }

    [Fact]
    public async Task Item_no_limite_do_raio_vale_e_alem_dele_e_longe_demais()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Sorteador.Resultado = [Espada];
        DerrotarInimigoPertoDe(ana, RegrasLoot.RaioDeColetaMetros - 0.01, ana);
        var perto = (await ApareceuAsync(ana)).Loot;
        DerrotarInimigoPertoDe(ana, RegrasLoot.RaioDeColetaMetros + 0.5, ana);
        var longe = (await ApareceuAsync(ana)).Loot;

        Coletar(ana, longe.LootId);
        var recusa = await RecusadaAsync(ana);
        Assert.Equal(longe.LootId, recusa.LootId);
        Assert.Equal(CodigosErroItens.LongeDemais, recusa.Codigo);

        Coletar(ana); // Só o que está ao alcance entra.
        Assert.Equal(perto.LootId, (await ColetadoAsync(ana)).LootId);
        Assert.Single(await InventarioAsync(ana));
        Assert.Equal(1, NoChaoDe(ana)); // O de longe continua lá, sem perda.
    }

    [Fact]
    public async Task Item_de_outro_jogador_ou_id_inventado_nao_e_encontrado()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        var daAna = (await LargarParaAsync(ana, Espada))[0];

        Coletar(beto, daAna.LootId);
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(beto)).Codigo);
        Coletar(beto, long.MaxValue);
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(beto)).Codigo);
        Coletar(beto, -1);
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(beto)).Codigo);
        Coletar(beto); // O "tudo por perto" do Beto não enxerga o item da Ana.
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(beto)).Codigo);

        Assert.Empty(await InventarioAsync(beto));
        Assert.Equal(1, NoChaoDe(ana));
    }

    [Fact]
    public async Task Dois_pedidos_do_mesmo_item_ao_mesmo_tempo_dao_um_item_so()
    {
        using var ana = await NovoJogadorAsync();
        var loot = (await LargarParaAsync(ana, Espada))[0];

        Coletar(ana, loot.LootId);
        Coletar(ana, loot.LootId);
        Coletar(ana);

        await TempoRealFixture.EsperarAsync(
            () =>
                ana.Recebidas(ItensMessageIds.LootColetado) == 1
                && ana.Recebidas(ItensMessageIds.ColetaRecusada) == 2,
            "uma coleta aceita e duas recusadas"
        );
        Assert.Equal(new[] { Espada }, await InventarioAsync(ana));
        Assert.Equal(0, NoChaoDe(ana));
    }

    [Fact]
    public async Task Inventario_quase_cheio_recebe_o_que_cabe_e_o_resto_fica_no_chao()
    {
        using var ana = await NovoJogadorAsync();
        await EncherInventarioAsync(ana, RegrasItens.EspacosInventario - 1);
        var loot = await LargarParaAsync(ana, Espada, Escudo);

        Coletar(ana);

        Assert.Equal(loot[0].LootId, (await ColetadoAsync(ana)).LootId);
        var recusa = await RecusadaAsync(ana);
        Assert.Equal(RegrasLoot.TudoPorPerto, recusa.LootId);
        Assert.Equal(CodigosErroItens.InventarioCheio, recusa.Codigo);
        Assert.Equal(RegrasItens.EspacosInventario, (await InventarioAsync(ana)).Length);
        Assert.Equal(1, NoChaoDe(ana));
        Assert.Equal(1, ana.Recebidas(ItensMessageIds.LootColetado));
    }

    [Fact]
    public async Task Inventario_cheio_mantem_o_item_no_chao_sem_perda()
    {
        using var ana = await NovoJogadorAsync();
        await EncherInventarioAsync(ana, RegrasItens.EspacosInventario);
        var loot = (await LargarParaAsync(ana, Pocao))[0];

        Coletar(ana, loot.LootId);

        var recusa = await RecusadaAsync(ana);
        Assert.Equal(loot.LootId, recusa.LootId);
        Assert.Equal(CodigosErroItens.InventarioCheio, recusa.Codigo);
        Assert.DoesNotContain(Pocao, await InventarioAsync(ana));
        Assert.Equal(1, NoChaoDe(ana));
        Assert.False(ana.Recebeu(ItensMessageIds.LootColetado));
    }

    [Fact]
    public async Task Item_expira_no_fim_da_reserva_e_avisa_o_dono()
    {
        using var ana = await NovoJogadorAsync();
        var loot = (await LargarParaAsync(ana, Espada))[0];
        var loots = servidor.Servico<ServicoDeLoot>();

        servidor.Relogio.Avancar(RegrasLoot.ReservaSegundos - 1);
        loots.Expirar();
        Assert.Equal(1, NoChaoDe(ana)); // Ainda no prazo.

        servidor.Relogio.Avancar(1);
        loots.Expirar();

        var sumiu = await ana.Cliente.ReceberAsync<LootSumiu>(ItensMessageIds.LootSumiu);
        Assert.Equal(loot.LootId, sumiu.LootId);
        Coletar(ana, loot.LootId);
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(ana)).Codigo);
        Assert.Empty(await InventarioAsync(ana));
    }

    [Fact]
    public async Task Dono_que_sai_e_volta_ainda_ve_o_loot_dentro_do_prazo()
    {
        using var ana = await NovoJogadorAsync();
        var loot = (await LargarParaAsync(ana, Escudo))[0];

        ana.Cliente.Sair();
        var tempoReal = servidor.Servico<ServidorTempoReal>();
        await TempoRealFixture.EsperarAsync(
            () => tempoReal.Sessoes.All(s => s.PersonagemId != ana.Id),
            "a sessão antiga encerrar"
        );
        servidor.Relogio.Avancar(30);
        using var deVolta = await ConectarNaCidadeAsync(ana.Http, ana.Id);

        var visto = await deVolta.ReceberAsync<LootApareceu>(ItensMessageIds.LootApareceu);
        Assert.Equal(loot.LootId, visto.Loot.LootId);
        Assert.Equal(Escudo, visto.Loot.ItemId);
        Assert.Equal(RegrasLoot.ReservaSegundos - 30, visto.Loot.ExpiraEmSegundos);
    }

    [Fact]
    public async Task Dono_fora_do_jogo_recebe_o_loot_ao_entrar()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        beto.Cliente.Sair();
        var tempoReal = servidor.Servico<ServidorTempoReal>();
        await TempoRealFixture.EsperarAsync(
            () => tempoReal.Sessoes.All(s => s.PersonagemId != beto.Id),
            "a sessão do Beto encerrar"
        );
        servidor.Sorteador.Resultado = [Pocao];

        DerrotarInimigoPertoDe(ana, 0, ana, beto);

        await ApareceuAsync(ana);
        using var deVolta = await ConectarNaCidadeAsync(beto.Http, beto.Id);
        var visto = await deVolta.ReceberAsync<LootApareceu>(ItensMessageIds.LootApareceu);
        Assert.Equal(Pocao, visto.Loot.ItemId);
    }

    [Fact]
    public async Task Servico_repassa_variante_categoria_e_nivel_ao_sorteio()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Sorteador.Quedas = [];

        DerrotarInimigoPertoDe(
            ana,
            0,
            CatalogoCriaturas.LoboDoCampo,
            VarianteDeCriatura.Elite,
            12,
            ana
        );
        await EsperarAsync(() => servidor.Sorteador.UltimoNivel == 12);
        Assert.Equal(OrigemDoLoot.Elite, servidor.Sorteador.UltimaOrigem);

        DerrotarInimigoPertoDe(
            ana,
            0,
            CatalogoCriaturas.PresaVelha,
            VarianteDeCriatura.Normal,
            0,
            ana
        );
        await EsperarAsync(() => servidor.Sorteador.UltimaOrigem == OrigemDoLoot.ChefeRaro);
        Assert.Equal(5, servidor.Sorteador.UltimoNivel); // Nível 0: vale o da ficha.

        DerrotarInimigoPertoDe(
            ana,
            0,
            CatalogoCriaturas.GuardiaoDoReflexo,
            VarianteDeCriatura.Normal,
            5,
            ana
        );
        await EsperarAsync(() => servidor.Sorteador.UltimaOrigem == OrigemDoLoot.Chefe);
    }

    [Fact]
    public async Task Peca_larga_pelo_mob_entra_no_inventario_com_tier_e_o_grau_da_queda()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Sorteador.Quedas =
        [
            new QuedaDeLoot
            {
                ModeloId = CatalogoEquipamentos.PrimeiroModelo,
                Origem = OrigemDoLoot.Chefe,
                Grau = GrauEquipamento.Medio,
            },
        ];
        DerrotarInimigoPertoDe(ana, 0, ana);

        var loot = (await ApareceuAsync(ana)).Loot;
        Assert.Equal(CatalogoEquipamentos.PrimeiroModelo, loot.ItemId);
        Assert.Equal("Elmo do Vigia", loot.Nome);
        Assert.Equal(GrauEquipamento.Medio, loot.Grau);

        Coletar(ana, loot.LootId);
        var coletado = await ColetadoAsync(ana);
        Assert.Equal("Elmo do Vigia", coletado.Entrada.Nome);
        var peca = coletado.Entrada.Equipamento!;
        Assert.Equal(GrauEquipamento.Medio, peca.Grau);
        // O servidor sorteou os valores dentro da faixa do grau Médio (20 a 80) e gravou o tier de origem.
        Assert.Equal(RegrasTier.VersaoAtual, peca.VersaoDosAtributos);
        Assert.All(peca.Atributos, a => Assert.InRange(a.Valor, 20, 80));
        Assert.Equal(RegrasTier.TierDe(peca), peca.TierOrigem);
        Assert.Equal(10, coletado.Entrada.Equipamento.NivelPersonagemMinimo);
    }

    [Fact]
    public async Task Peca_no_chao_ja_traz_o_tier_e_e_a_mesma_que_entra_no_inventario()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Sorteador.Quedas =
        [
            new QuedaDeLoot
            {
                ModeloId = CatalogoEquipamentos.PrimeiroModelo,
                Origem = OrigemDoLoot.Chefe,
                Grau = GrauEquipamento.Forte,
            },
        ];
        DerrotarInimigoPertoDe(ana, 0, ana);
        var loot = (await ApareceuAsync(ana)).Loot;
        Assert.True(loot.TemTier);

        Coletar(ana, loot.LootId);
        var coletado = await ColetadoAsync(ana);

        Assert.Equal(loot.Tier, RegrasTier.TierDe(coletado.Entrada.Equipamento!));
        servidor.Sorteador.Quedas = null;
    }

    [Fact]
    public async Task Filtro_de_tier_deixa_no_chao_a_peca_abaixo_e_coleta_a_igual_ou_acima()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Sorteador.Quedas =
        [
            new QuedaDeLoot
            {
                ModeloId = CatalogoEquipamentos.PrimeiroModelo,
                Origem = OrigemDoLoot.Comum,
                Grau = GrauEquipamento.Inicial,
            },
        ];
        DerrotarInimigoPertoDe(ana, 0, ana);
        var loot = (await ApareceuAsync(ana)).Loot;
        servidor.Sorteador.Quedas = null;

        // Exigir S+ (a peça não tem): fica no chão e o pedido não acha nada.
        ana.Cliente.Enviar(
            ItensMessageIds.ColetarLoot,
            new ColetarLoot { TierMinimo = TierItem.SMais }
        );
        Assert.Equal(CodigosErroItens.LootNaoEncontrado, (await RecusadaAsync(ana)).Codigo);
        Assert.Equal(1, NoChaoDe(ana));
        Assert.Empty(await InventarioAsync(ana));

        // Exigir exatamente o tier da peça: coleta.
        ana.Cliente.Enviar(ItensMessageIds.ColetarLoot, new ColetarLoot { TierMinimo = loot.Tier });
        Assert.Equal(loot.LootId, (await ColetadoAsync(ana)).LootId);
        Assert.Equal(0, NoChaoDe(ana));
    }

    [Fact]
    public async Task Filtro_de_tier_nao_barra_item_sem_tier_nem_o_pedido_de_um_item_so()
    {
        using var ana = await NovoJogadorAsync();
        var loot = await LargarParaAsync(ana, Pocao);
        Assert.False(loot[0].TemTier);

        ana.Cliente.Enviar(
            ItensMessageIds.ColetarLoot,
            new ColetarLoot { TierMinimo = TierItem.SMais }
        );

        Assert.Equal(loot[0].LootId, (await ColetadoAsync(ana)).LootId);
    }

    private static async Task EsperarAsync(Func<bool> condicao)
    {
        for (var i = 0; i < 200 && !condicao(); i++)
        {
            await Task.Delay(25);
        }

        Assert.True(condicao());
    }
}
