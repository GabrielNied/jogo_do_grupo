using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Features.Pets;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Pets;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Pets;

/// <summary>
/// Servidor de teste dos pets: banco real, Mundo de verdade e sorteio de loot controlado. O
/// relógio é o real: o laço do tempo real agenda os passos por ele, e com o relógio parado o pet
/// nunca andaria.
/// </summary>
public sealed class ServidorDePets : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public SorteadorDeLootFalso Sorteador { get; } = new();
    public Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorDePets()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<ISorteadorDeLoot>(Sorteador))
        );
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
    }

    public T Servico<T>()
        where T : notnull => Falso.Services.GetRequiredService<T>();

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

/// <summary>
/// Integração dos pets (docs/features/pets.md): client UDP de verdade, banco SQLite e o Itens
/// que já existe. O movimento em si é testado sem rede em <c>RegrasPetsTestes</c>.
/// </summary>
public class PetsTestes(ServidorDePets servidor) : IClassFixture<ServidorDePets>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Escudo = CatalogoItens.EscudoDeTeste;
    private const int Pocao = CatalogoItens.PocaoDeTeste;

    private static long _mortes;

    private async Task<Jogador> NovoJogadorAsync()
    {
        servidor.Sorteador.Quedas = null;
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            servidor.Porta,
            http.DefaultRequestHeaders.Authorization!.Parameter!,
            personagem.Id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return new Jogador(http, cliente, personagem.Id);
    }

    private PosicaoNoMundo PosicaoDe(Jogador j)
    {
        Assert.True(servidor.Servico<IPosicoesDoMundo>().TentarObter(j.Id, out var posicao));
        return posicao;
    }

    private void Teleportar(Jogador j, float x, float z) =>
        Assert.True(
            servidor.Servico<ITeleporteDeTeste>().Teleportar(j.Id, x, 0f, z).Aceito,
            "teleporte recusado"
        );

    private static void Chamar(Jogador j) =>
        j.Cliente.Enviar(PetsMessageIds.ChamarPet, new ChamarPet());

    private static Task<PetApareceu> ApareceuAsync(Jogador j) =>
        j.Cliente.ReceberAsync<PetApareceu>(PetsMessageIds.PetApareceu);

    private int Pets => servidor.Servico<ServicoDePets>().Quantidade;

    private async Task<float> DistanciaPetDonoAsync(Jogador dono, PetApareceu pet)
    {
        var posicao = PosicaoDe(dono);
        await Task.CompletedTask;
        return RegrasPets.Distancia(pet.X, pet.Z, posicao.X, posicao.Z);
    }

    /// <summary>Deixa um item do dono no chão, a <paramref name="distancia"/> metros dele (+X).</summary>
    private async Task<LootNoChao> LargarAsync(Jogador dono, int item, double distancia)
    {
        servidor.Sorteador.Resultado = [item];
        var posicao = PosicaoDe(dono);
        servidor
            .Servico<EventosDoServidor>()
            .Publicar(
                new InimigoDerrotado(
                    1,
                    RegrasInimigo.CriaturaDaFatia,
                    VarianteDeCriatura.Normal,
                    40,
                    1.0,
                    posicao.Area,
                    posicao.X + distancia,
                    posicao.Z,
                    [dono.Id],
                    Interlocked.Increment(ref _mortes),
                    0
                )
            );
        return (await dono.Cliente.ReceberAsync<LootApareceu>(ItensMessageIds.LootApareceu)).Loot;
    }

    private async Task<int[]> InventarioAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasItens.InventarioDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        var inventario = await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resposta);
        return inventario.Entradas.Select(e => e.ItemId).ToArray();
    }

    private int NoChaoDe(Jogador j) =>
        servidor.Servico<ChaoDeLoot>().DoDono(j.Id, TimeProvider.System.GetUtcNow()).Count;

    [Fact]
    public async Task Chamar_na_cidade_cria_o_pet_a_dois_metros_do_dono()
    {
        using var ana = await NovoJogadorAsync();

        Chamar(ana);

        var pet = await ApareceuAsync(ana);
        Assert.Equal(ana.Id, pet.DonoId);
        Assert.Equal(EspecieDePet.CervoJovem, pet.Especie);
        Assert.Equal(ConfigPets.DistanciaDeSeguidaMetros, await DistanciaPetDonoAsync(ana, pet), 2);
    }

    [Fact]
    public async Task Chamar_de_novo_nao_duplica_o_pet()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        Chamar(ana);
        Chamar(ana);
        await Task.Delay(400);

        Assert.Equal(1, ana.Recebidas(PetsMessageIds.PetApareceu));
        Assert.Equal(1, Pets);
    }

    [Fact]
    public async Task Pet_segue_o_dono_e_para_a_dois_metros()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        var posicao = PosicaoDe(ana);
        Teleportar(ana, posicao.X + 12f, posicao.Z);

        // Chega e para: o último PetMudou diz que ele não anda mais, a dois metros do dono.
        PetMudou ultimo = null!;
        while (true)
        {
            ultimo = await ana.Cliente.ReceberAsync<PetMudou>(PetsMessageIds.PetMudou);
            if (!ultimo.Andando)
            {
                break;
            }
        }

        var dono = PosicaoDe(ana);
        Assert.Equal(
            ConfigPets.DistanciaDeSeguidaMetros,
            RegrasPets.Distancia(ultimo.X, ultimo.Z, dono.X, dono.Z),
            1
        );
    }

    [Fact]
    public async Task Dono_longe_demais_faz_o_pet_voltar_para_perto_dele()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        var posicao = PosicaoDe(ana);
        Teleportar(ana, posicao.X + ConfigPets.DistanciaParaAlcancarMetros + 10f, posicao.Z);

        var mudou = await ana.Cliente.ReceberAsync<PetMudou>(PetsMessageIds.PetMudou);
        var dono = PosicaoDe(ana);
        Assert.Equal(
            ConfigPets.DistanciaDeSeguidaMetros,
            RegrasPets.Distancia(mudou.X, mudou.Z, dono.X, dono.Z),
            1
        );
    }

    [Fact]
    public async Task Dispensar_remove_o_pet_e_dispensar_sem_pet_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        ana.Cliente.Enviar(PetsMessageIds.DispensarPet, new DispensarPet());
        var sumiu = await ana.Cliente.ReceberAsync<PetSumiu>(PetsMessageIds.PetSumiu);
        Assert.Equal(ana.Id, sumiu.DonoId);
        Assert.Equal(0, Pets);

        ana.Cliente.Enviar(PetsMessageIds.DispensarPet, new DispensarPet());
        var recusa = await ana.Cliente.ReceberAsync<PetRecusado>(PetsMessageIds.PetRecusado);
        Assert.Equal(CodigosErroPets.SemPet, recusa.Codigo);
    }

    [Fact]
    public async Task Entrar_numa_caverna_remove_o_pet_e_la_nao_da_para_chamar()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        var movido = servidor
            .Servico<ITransicoesDeAreaDoMundo>()
            .TentarMoverParaArea(
                ana.Id,
                new PosicaoNoMundo(AreasDoMundo.Instancia("pets-teste"), 0, 0, 0, 0)
            );
        Assert.True(movido);

        await ana.Cliente.ReceberAsync<PetSumiu>(PetsMessageIds.PetSumiu);
        Assert.Equal(0, Pets);

        Chamar(ana);
        var recusa = await ana.Cliente.ReceberAsync<PetRecusado>(PetsMessageIds.PetRecusado);
        Assert.Equal(CodigosErroPets.ForaDaCidade, recusa.Codigo);
        Assert.Equal(0, Pets);
    }

    [Fact]
    public async Task Quem_esta_perto_ve_o_pet_do_outro_e_quem_esta_longe_nao()
    {
        using var ana = await NovoJogadorAsync();
        using var bia = await NovoJogadorAsync();
        using var caio = await NovoJogadorAsync();
        var posicao = PosicaoDe(ana);
        Teleportar(bia, posicao.X + 5f, posicao.Z);
        Teleportar(caio, 900f, 900f);

        Chamar(ana);

        var visto = await ApareceuAsync(bia);
        Assert.Equal(ana.Id, visto.DonoId);
        await Task.Delay(400);
        Assert.False(caio.Recebeu(PetsMessageIds.PetApareceu));
    }

    [Fact]
    public async Task Pet_some_para_os_outros_quando_o_dono_desconecta()
    {
        using var ana = await NovoJogadorAsync();
        using var bia = await NovoJogadorAsync();
        var posicao = PosicaoDe(ana);
        Teleportar(bia, posicao.X + 5f, posicao.Z);
        Chamar(ana);
        await ApareceuAsync(bia);

        ana.Cliente.Sair();

        var sumiu = await bia.Cliente.ReceberAsync<PetSumiu>(PetsMessageIds.PetSumiu);
        Assert.Equal(ana.Id, sumiu.DonoId);
        await TempoRealFixture.EsperarAsync(() => Pets == 0, "o pet do dono desconectado sumir");
    }

    [Fact]
    public async Task Pet_junta_o_loot_do_dono_e_o_item_entra_no_inventario()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        var loot = await LargarAsync(ana, Espada, 3);

        var coletado = await ana.Cliente.ReceberAsync<LootColetado>(ItensMessageIds.LootColetado);
        Assert.Equal(loot.LootId, coletado.LootId);
        Assert.Equal(new[] { Espada }, await InventarioAsync(ana));
        Assert.Equal(0, NoChaoDe(ana));
        Assert.False(ana.Recebeu(ItensMessageIds.ColetaRecusada));
    }

    [Fact]
    public async Task Pet_junta_um_item_por_vez_ate_acabar()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        await LargarAsync(ana, Espada, 3);
        await LargarAsync(ana, Escudo, 4);

        await TempoRealFixture.EsperarAsync(
            () => ana.Recebidas(ItensMessageIds.LootColetado) == 2,
            "o pet juntar os dois itens"
        );
        Assert.Equal(new[] { Espada, Escudo }, (await InventarioAsync(ana)).Order());
        Assert.Equal(0, NoChaoDe(ana));
    }

    [Fact]
    public async Task Item_alem_de_oito_metros_do_pet_fica_no_chao()
    {
        using var ana = await NovoJogadorAsync();
        Chamar(ana);
        await ApareceuAsync(ana);

        await LargarAsync(ana, Espada, ConfigPets.RaioDeBuscaDeLootMetros + 6);
        await Task.Delay(1500);

        Assert.False(ana.Recebeu(ItensMessageIds.LootColetado));
        Assert.Equal(1, NoChaoDe(ana));
    }

    [Fact]
    public async Task Inventario_cheio_deixa_o_item_no_chao_sem_perda_e_sem_aviso()
    {
        using var ana = await NovoJogadorAsync();
        using (var escopo = servidor.Falso.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            for (var i = 0; i < RegrasItens.EspacosInventario; i++)
            {
                db.Add(
                    new ItemInventario
                    {
                        PersonagemId = ana.Id,
                        ItemId = CatalogoItens.MoedaDeTeste,
                    }
                );
            }

            await db.SaveChangesAsync();
        }

        Chamar(ana);
        await ApareceuAsync(ana);

        await LargarAsync(ana, Pocao, 3);
        await Task.Delay(2000);

        Assert.False(ana.Recebeu(ItensMessageIds.LootColetado));
        Assert.False(ana.Recebeu(ItensMessageIds.ColetaRecusada));
        Assert.DoesNotContain(Pocao, await InventarioAsync(ana));
        Assert.Equal(1, NoChaoDe(ana));
    }

    [Fact]
    public async Task Pet_nao_leva_o_item_de_outro_dono()
    {
        using var ana = await NovoJogadorAsync();
        using var bia = await NovoJogadorAsync();
        var posicao = PosicaoDe(ana);
        Teleportar(bia, posicao.X + 40f, posicao.Z);
        Chamar(ana);
        await ApareceuAsync(ana);

        // O loot é da Bia, mas cai ao lado do pet da Ana.
        servidor.Sorteador.Resultado = [Espada];
        servidor
            .Servico<EventosDoServidor>()
            .Publicar(
                new InimigoDerrotado(
                    1,
                    RegrasInimigo.CriaturaDaFatia,
                    VarianteDeCriatura.Normal,
                    40,
                    1.0,
                    posicao.Area,
                    posicao.X + 3,
                    posicao.Z,
                    [bia.Id],
                    Interlocked.Increment(ref _mortes),
                    0
                )
            );
        await Task.Delay(1500);

        Assert.Equal(1, NoChaoDe(bia));
        Assert.Empty(await InventarioAsync(ana));
        Assert.Empty(await InventarioAsync(bia));
        Assert.False(ana.Recebeu(ItensMessageIds.LootColetado));
    }

    [Fact]
    public async Task Pet_e_dono_coletando_o_mesmo_item_ao_mesmo_tempo_dao_um_item_so()
    {
        using var ana = await NovoJogadorAsync();
        var itens = servidor.Servico<ILootDoDono>();
        const int rodadas = 8;

        for (var i = 0; i < rodadas; i++)
        {
            var loot = await LargarAsync(ana, Espada, 1);

            // O dono aperta F no item enquanto o pet pede a coleta do mesmo item.
            ana.Cliente.Enviar(
                ItensMessageIds.ColetarLoot,
                new ColetarLoot { LootId = loot.LootId }
            );
            var doPet = await itens.ColetarParaODono(ana.Id, loot.LootId);

            await TempoRealFixture.EsperarAsync(
                () => ana.Recebidas(ItensMessageIds.LootColetado) == i + 1 && NoChaoDe(ana) == 0,
                "um só dos dois levar o item"
            );
            Assert.NotEqual(ResultadoDaColetaParaODono.SemEspaco, doPet);
        }

        await Task.Delay(300);
        Assert.Equal(rodadas, ana.Recebidas(ItensMessageIds.LootColetado));
        Assert.Equal(rodadas, (await InventarioAsync(ana)).Length);
    }

    [Fact]
    public async Task Dois_pedidos_do_pet_para_o_mesmo_item_dao_um_item_so()
    {
        using var ana = await NovoJogadorAsync();
        var itens = servidor.Servico<ILootDoDono>();
        var loot = await LargarAsync(ana, Espada, 1);

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 6)
                .Select(_ => Task.Run(() => itens.ColetarParaODono(ana.Id, loot.LootId)))
        );

        Assert.Equal(1, resultados.Count(r => r == ResultadoDaColetaParaODono.Coletado));
        Assert.Equal(5, resultados.Count(r => r == ResultadoDaColetaParaODono.Indisponivel));
        Assert.Equal(new[] { Espada }, await InventarioAsync(ana));
        Assert.Equal(0, NoChaoDe(ana));
    }

    [Fact]
    public async Task Pedido_do_pet_para_item_de_outro_dono_ou_inventado_e_indisponivel()
    {
        using var ana = await NovoJogadorAsync();
        using var bia = await NovoJogadorAsync();
        var itens = servidor.Servico<ILootDoDono>();
        var loot = await LargarAsync(bia, Espada, 1);

        Assert.Equal(
            ResultadoDaColetaParaODono.Indisponivel,
            await itens.ColetarParaODono(ana.Id, loot.LootId)
        );
        Assert.Equal(
            ResultadoDaColetaParaODono.Indisponivel,
            await itens.ColetarParaODono(ana.Id, 987654321)
        );
        Assert.Equal(1, NoChaoDe(bia));
        Assert.Empty(await InventarioAsync(ana));
    }
}
