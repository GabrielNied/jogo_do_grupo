using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Contas;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>Alvo de mentira: fica sempre no volume enquanto <see cref="Ativo"/>.</summary>
public sealed class AlvoFalso(long id) : IAlvosDeCombate
{
    private int _aplicacoes;

    public volatile bool Ativo;
    public int Aplicacoes => Volatile.Read(ref _aplicacoes);

    /// <summary>
    /// Quando (<see cref="Environment.TickCount64"/>) o servidor procurou alvos: o momento do
    /// acerto, medido no próprio servidor (sem a rede nem o client no meio).
    /// </summary>
    public ConcurrentQueue<long> Consultas { get; } = new();

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        Consultas.Enqueue(Environment.TickCount64);
        return Ativo ? [new AlvoDeCombate(id, origemX, origemZ, 0.5, 100, 100)] : [];
    }

    public IReadOnlyList<AlvoDeCombate> AlvosProximos(
        string area,
        double origemX,
        double origemZ,
        double alcanceMetros,
        long instanteReferenciaMs = 0
    ) => AlvosNoVolume(area, origemX, origemZ, 0, instanteReferenciaMs);

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        if (alvoId != id)
        {
            return false;
        }

        Interlocked.Increment(ref _aplicacoes);
        resultado = new ResultadoDeDano(id, dano, 100 - dano, 100);
        return true;
    }
}

/// <summary>O Mundo de mentira que só anota os limites de velocidade que Combate pediu.</summary>
public sealed class LimiteDeVelocidadeFalso : ILimiteDeVelocidadeDoMundo
{
    public ConcurrentQueue<(
        long PersonagemId,
        double Fator,
        double DuracaoSegundos
    )> Pedidos { get; } = new();

    public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos)
    {
        Pedidos.Enqueue((personagemId, fator, duracaoSegundos));
        return true;
    }
}

/// <summary>
/// Servidor de teste do ataque: banco real, relógio de recursos e bônus falsos, um boneco de
/// treino de relógio controlável e dois alvos extras (um com id novo, um com o id do boneco).
/// </summary>
public sealed class ServidorDeAtaque : IDisposable
{
    private long _relogioBonecoMs = 1_000;

    public ServidorDeTeste Real { get; } = new();
    public RelogioFalso Relogio { get; } = new();
    public BonusFalso Bonus { get; } = new();
    public BonecoDeTreino Boneco { get; }
    public LimiteDeVelocidadeFalso Limite { get; } = new();
    public AlvoFalso Extra { get; } = new(-50);
    public AlvoFalso RepetidoDoBoneco { get; } = new(ConfigBonecoDeTreino.AlvoId);
    public WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorDeAtaque()
    {
        Boneco = new BonecoDeTreino(() => Interlocked.Read(ref _relogioBonecoMs));
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(Relogio);
                s.AddSingleton<IBonusDeAtributos>(Bonus);
                s.AddSingleton<IAtributosDosEquipamentos>(Bonus);
                s.AddSingleton<ILimiteDeVelocidadeDoMundo>(Limite);
                s.RemoveAll<IAlvosDeCombate>();
                s.AddSingleton<IAlvosDeCombate>(Boneco);
                s.AddSingleton<IAlvosDeCombate>(Extra);
                s.AddSingleton<IAlvosDeCombate>(RepetidoDoBoneco);
            })
        );
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
    }

    /// <summary>Deixa o boneco com a vida cheia (passa o tempo de recuperação).</summary>
    public void ResetarBoneco() =>
        Interlocked.Add(
            ref _relogioBonecoMs,
            (long)((ConfigBonecoDeTreino.SegundosParaRecuperar + 1) * 1000.0)
        );

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

/// <summary>Um jogador de verdade: personagem criado, conectado e (se pedido) na cidade.</summary>
public sealed class Jogador(HttpClient http, ClienteTempoRealDeTeste cliente, long id) : IDisposable
{
    public HttpClient Http { get; } = http;
    public ClienteTempoRealDeTeste Cliente { get; } = cliente;
    public long Id { get; } = id;

    public bool Recebeu(ushort mensagemId) => Cliente.IdsRecebidos.Contains(mensagemId);

    public int Recebidas(ushort mensagemId) => Cliente.IdsRecebidos.Count(x => x == mensagemId);

    public void Dispose()
    {
        Cliente.Dispose();
        Http.Dispose();
    }
}

/// <summary>
/// Integração do ataque da primeira arma: client UDP de verdade contra o servidor, com banco
/// SQLite e o MundoModule de verdade. Os tempos são os reais (preparo de 0,5 s no leve, 1 s no
/// pesado), então cada teste leva de 1 a 3 s.
/// </summary>
public class AtaqueTestes(ServidorDeAtaque servidor) : IClassFixture<ServidorDeAtaque>
{
    private static readonly TimeSpan Margem = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Espera até <paramref name="ms"/> depois de <paramref name="desde"/>; se a máquina carregada já
    /// passou desse ponto, segue na hora (um Delay negativo quebraria o teste).
    /// </summary>
    private static Task AteAsync(DateTime desde, int ms)
    {
        var falta = TimeSpan.FromMilliseconds(ms) - (DateTime.UtcNow - desde);
        return falta > TimeSpan.Zero ? Task.Delay(falta) : Task.CompletedTask;
    }

    private async Task<Jogador> NovoJogadorAsync(bool naCidade = true)
    {
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        servidor.Bonus.Efeitos = new AtributosCombate();
        servidor.Extra.Ativo = false;
        servidor.RepetidoDoBoneco.Ativo = false;
        servidor.ResetarBoneco();

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
        var jogador = new Jogador(http, cliente, personagem.Id);
        if (naCidade)
        {
            cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
            await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        }

        return jogador;
    }

    // O corpo do jogador vira de costas para o boneco (a mira e o corpo juntos, sem giro por limitar).
    private async Task VirarDeCostasAsync(Jogador jogador, int sequencia = 1)
    {
        jogador.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = ValidadorDeMovimento.Nascimento.X,
                Z = ValidadorDeMovimento.Nascimento.Z,
                RotacaoY = 180f,
                Sequencia = sequencia,
            }
        );
        await PosicaoNoServidor.EsperarAsync(
            servidor.Falso.Services,
            jogador.Id,
            p => p.RotacaoY == 180f
        );
    }

    private static void Atacar(
        Jogador j,
        TipoDeAtaque tipo = TipoDeAtaque.Leve,
        int sequencia = 1,
        float direcao = 0f
    ) =>
        j.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque
            {
                Tipo = tipo,
                DirecaoY = direcao,
                Sequencia = sequencia,
            }
        );

    private static async Task<RecursosResponse> RecursosAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasCombate.RecursosDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    /// <summary>Muda o que está guardado dos recursos (cria a linha se for a primeira vez).</summary>
    private async Task DefinirRecursosAsync(Jogador j, int? vida = null, int? stamina = null)
    {
        await RecursosAsync(j);
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id); // O banco precisa ter o que a memória já mudou.
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        if (vida != null)
        {
            r.Vida = vida.Value;
        }

        if (stamina != null)
        {
            var agora = servidor.Relogio.GetUtcNow().UtcDateTime;
            r.Stamina = stamina.Value;
            r.UltimoGastoStaminaEm = agora;
            r.StaminaContadaAte = agora;
        }

        await db.SaveChangesAsync();
        memoria.Descartar(j.Id); // E a memória precisa reler o que o teste mudou.
    }

    private static Task EsperarRecusaAsync(Jogador j, int quantidade = 1) =>
        TempoRealFixture.EsperarAsync(
            () => j.Recebidas(CombateMessageIds.AtaqueRecusado) >= quantidade,
            "AtaqueRecusado"
        );

    [Fact]
    public async Task Leve_avisa_o_preparo_e_acerta_o_boneco_com_o_dano_da_espada()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();

        Atacar(ana);

        var iniciado = await ana.Cliente.ReceberAsync<AtaqueIniciado>(
            CombateMessageIds.AtaqueIniciado
        );
        Assert.Equal(ana.Id, iniciado.AtacanteId);
        Assert.Equal(TipoDeAtaque.Leve, iniciado.Tipo);
        Assert.Equal(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve), iniciado.PreparoMs);

        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        Assert.Equal(ana.Id, acertou.AtacanteId);
        var acerto = Assert.Single(acertou.Acertos);
        Assert.Equal(ConfigBonecoDeTreino.AlvoId, acerto.AlvoId);
        Assert.Equal(RegrasAtaque.Dano(TipoDeAtaque.Leve, 0), acerto.Dano);
        Assert.Equal(ConfigBonecoDeTreino.Vida - acerto.Dano, acerto.VidaRestante);
        Assert.Equal(ConfigBonecoDeTreino.Vida, acerto.VidaMaxima);
        Assert.Equal(acerto.VidaRestante, servidor.Boneco.VidaAtual);

        // Quem está na área vê o preparo e o acerto; o leve não gasta nada, então ninguém
        // recebe mudança de recursos.
        await beto.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await beto.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        Assert.False(ana.Recebeu(CombateMessageIds.RecursosMudaram));
        Assert.False(beto.Recebeu(CombateMessageIds.AtaqueRecusado));
    }

    [Fact]
    public async Task Preparo_so_vale_depois_do_tempo_de_preparo()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        var inicio = DateTime.UtcNow;
        await Task.Delay(150);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueAcertou));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);

        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        Assert.True(DateTime.UtcNow - inicio >= TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Pesado_gasta_20_de_stamina_uma_vez_so_e_avisa_so_o_dono()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();

        Atacar(ana, TipoDeAtaque.Pesado);

        var recursos = await ana.Cliente.ReceberAsync<RecursosMudaram>(
            CombateMessageIds.RecursosMudaram
        );
        Assert.Equal(
            RegrasRecursos.StaminaBase - RegrasAtaque.CustoDeStaminaPesado,
            recursos.StaminaAtual
        );
        Assert.Equal(RegrasRecursos.StaminaBase, recursos.StaminaMaxima);
        Assert.Equal(RegrasRecursos.VidaBase, recursos.VidaAtual);

        var iniciado = await ana.Cliente.ReceberAsync<AtaqueIniciado>(
            CombateMessageIds.AtaqueIniciado
        );
        Assert.Equal(TipoDeAtaque.Pesado, iniciado.Tipo);
        Assert.Equal(1000, iniciado.PreparoMs);
        var acerto = Assert.Single(
            (await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou)).Acertos
        );
        Assert.Equal(RegrasAtaque.Dano(TipoDeAtaque.Pesado, 0), acerto.Dano);
        Assert.Equal(44, acerto.Dano);

        Assert.Equal(80, (await RecursosAsync(ana)).StaminaAtual);
        Assert.False(beto.Recebeu(CombateMessageIds.RecursosMudaram));
    }

    [Fact]
    public async Task Dois_pesados_no_mesmo_instante_cobram_o_stamina_uma_vez_e_um_so_comeca()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, TipoDeAtaque.Pesado, sequencia: 1);
        Atacar(ana, TipoDeAtaque.Pesado, sequencia: 2);

        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(2, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusa.Codigo);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);

        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueIniciado));
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.RecursosMudaram));
        Assert.Equal(80, (await RecursosAsync(ana)).StaminaAtual);
    }

    [Fact]
    public async Task Pesado_sem_stamina_e_recusado_sem_gastar_nada_e_o_leve_ainda_pode()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 19);

        Atacar(ana, TipoDeAtaque.Pesado, sequencia: 5);
        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );

        Assert.Equal(5, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.StaminaInsuficiente, recusa.Codigo);
        Assert.Equal(19, (await RecursosAsync(ana)).StaminaAtual);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);

        // Recusado de verdade liberou o personagem: o leve (custo 0) começa na hora.
        await DefinirRecursosAsync(ana, stamina: 0);
        Atacar(ana, TipoDeAtaque.Leve, sequencia: 6);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        Assert.Equal(0, (await RecursosAsync(ana)).StaminaAtual);
    }

    [Fact]
    public async Task Ataque_durante_atordoamento_e_recusado_com_VoceEstaAtordoado()
    {
        using var ana = await NovoJogadorAsync();
        var efeitos = servidor.Falso.Services.GetRequiredService<GerenciadorDeEfeitosDeStatus>();
        efeitos.Aplicar(ana.Id, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 5.0);

        Atacar(ana, TipoDeAtaque.Leve, sequencia: 1);

        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(1, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.VoceEstaAtordoado, recusa.Codigo);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
    }

    [Fact]
    public async Task Pesado_com_stamina_exato_passa_e_deixa_zero()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: RegrasAtaque.CustoDeStaminaPesado);

        Atacar(ana, TipoDeAtaque.Pesado);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);

        Assert.Equal(0, (await RecursosAsync(ana)).StaminaAtual);
    }

    [Fact]
    public async Task Personagem_caido_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, vida: 0);

        Atacar(ana, TipoDeAtaque.Leve, sequencia: 3);
        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );

        Assert.Equal(3, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.VoceEstaCaido, recusa.Codigo);
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
    }

    [Fact]
    public async Task Trocar_de_equipamento_atualiza_a_forca_no_golpe_seguinte()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, sequencia: 1);
        var primeiro = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        Assert.Equal(RegrasAtaque.Dano(TipoDeAtaque.Leve, 0), Assert.Single(primeiro.Acertos).Dano);

        // Os atributos guardados valem por um tempo curto; passado ele, o golpe seguinte relê. A
        // arma nova dá o dano-base (no lugar dos punhos).
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos
        {
            TemArma = true,
            DanoDaArma = 30,
        };
        servidor.Relogio.Avancar(ConfigRecursos.ValidadeDosAtributosSegundos + 0.1);
        await Task.Delay(TimeSpan.FromMilliseconds(500)); // Fim do golpe anterior.

        Atacar(ana, sequencia: 2);
        var segundo = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        Assert.Equal(30, Assert.Single(segundo.Acertos).Dano);
    }

    [Fact]
    public async Task Recursos_gastos_chegam_ao_banco_quando_o_personagem_desconecta()
    {
        var ana = await NovoJogadorAsync();
        Atacar(ana, TipoDeAtaque.Pesado);
        await ana.Cliente.ReceberAsync<RecursosMudaram>(CombateMessageIds.RecursosMudaram);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);

        // Em memória a stamina já baixou; o banco ainda não viu (a gravação periódica é de 5 s).
        ana.Dispose();

        var esperado = RegrasRecursos.StaminaBase - RegrasAtaque.CustoDeStaminaPesado;
        await TempoRealFixture.EsperarAsync(
            () => StaminaGravada(ana.Id) == esperado,
            "a stamina gasta gravada no banco"
        );
    }

    private int StaminaGravada(long personagemId)
    {
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        return db.Set<RecursosPersonagem>()
            .AsNoTracking()
            .Single(x => x.PersonagemId == personagemId)
            .Stamina;
    }

    [Fact]
    public async Task Forca_do_personagem_aumenta_o_dano_no_servidor()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Bonus.Efeitos = new AtributosCombate { Forca = 10 };

        Atacar(ana);
        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.Equal(RegrasAtaque.Dano(TipoDeAtaque.Leve, 10), Assert.Single(acertou.Acertos).Dano);
        Assert.Equal(40, acertou.Acertos[0].Dano);
    }

    [Fact]
    public async Task Dano_usa_a_arma_como_base_e_a_forca_do_item_e_do_personagem_em_parcelas_separadas()
    {
        using var ana = await NovoJogadorAsync();
        // Espada de 30 de dano, +15 flat do item e Força 2 do personagem (+20%): (30 + 15) x 1,20 = 54.
        servidor.Bonus.Efeitos = new AtributosCombate { Forca = 2 };
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos
        {
            TemArma = true,
            DanoDaArma = 30,
            BonusDanoFisico = 15,
            BonusDanoMagico = 18, // arma física: dano mágico do item não entra
        };

        Atacar(ana);
        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.Equal(54, Assert.Single(acertou.Acertos).Dano);
    }

    [Fact]
    public async Task Arma_magica_usa_o_intelecto_do_personagem_e_do_item()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Bonus.Efeitos = new AtributosCombate { Forca = 5, Intelecto = 3 };
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos
        {
            TemArma = true,
            ArmaMagica = true,
            DanoDaArma = 20,
            BonusDanoFisico = 18,
            BonusDanoMagico = 4,
        };

        Atacar(ana);
        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        // (20 + 4) x (1 + 0,30) = 31,2.
        Assert.Equal(31, Assert.Single(acertou.Acertos).Dano);
    }

    [Fact]
    public async Task Tres_leves_encadeados_o_terceiro_tira_1_2x_e_o_preparo_prende_a_corrida_a_40()
    {
        using var ana = await NovoJogadorAsync();

        // Cada pedido entra na fila nos últimos 0,3 s do golpe anterior (um leve dura 0,8 s).
        Atacar(ana, sequencia: 1);
        var primeiro = await ana.Cliente.ReceberAsync<AtaqueIniciado>(
            CombateMessageIds.AtaqueIniciado
        );
        var danos = new List<int>();
        var acertou1 = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        danos.Add(Assert.Single(acertou1.Acertos).Dano);
        Atacar(ana, sequencia: 2); // 0,5 s a 0,8 s: entra na fila
        var segundo = await ana.Cliente.ReceberAsync<AtaqueIniciado>(
            CombateMessageIds.AtaqueIniciado
        );
        var acertou2 = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        danos.Add(Assert.Single(acertou2.Acertos).Dano);
        Atacar(ana, sequencia: 3);
        var terceiro = await ana.Cliente.ReceberAsync<AtaqueIniciado>(
            CombateMessageIds.AtaqueIniciado
        );
        var acertou3 = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        danos.Add(Assert.Single(acertou3.Acertos).Dano);

        Assert.Equal([0, 1, 2], [primeiro.Passo, segundo.Passo, terceiro.Passo]);
        Assert.Equal(RegrasAtaque.Dano(TipoDeAtaque.Leve, 0, 2), danos[^1]);
        Assert.True(danos[^1] > danos[0]);
        Assert.Contains(
            servidor.Limite.Pedidos,
            p => p.PersonagemId == ana.Id && p.Fator == RegrasAtaque.VelocidadeDuranteGolpe
        );
    }

    [Fact]
    public async Task Pesado_cuja_mira_girou_180_graus_so_gira_60_e_o_arco_largo_ainda_pega_o_boneco()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, TipoDeAtaque.Pesado, direcao: 0f); // do nascimento o boneco está na frente
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = ValidadorDeMovimento.Nascimento.X,
                Z = ValidadorDeMovimento.Nascimento.Z,
                RotacaoY = 180f,
                Sequencia = 1,
            }
        );
        await PosicaoNoServidor.EsperarAsync(
            servidor.Falso.Services,
            ana.Id,
            p => p.RotacaoY == 180f
        );

        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Pesado) + Margem.Milliseconds);

        // Limitado a 60° em 1 s, o corpo ainda deixa o boneco dentro do arco largo do pesado
        // (110° de cada lado); o teto de giro em si é testado no shared (DirecaoDoGolpe).
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
    }

    [Fact]
    public async Task Fora_do_arco_nao_acerta_mas_o_ataque_acontece()
    {
        using var ana = await NovoJogadorAsync();
        await VirarDeCostasAsync(ana);

        Atacar(ana, direcao: 180f); // de costas para o boneco
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve) + Margem.Milliseconds);

        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueAcertou));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Fora_do_alcance_nao_acerta_e_vale_a_posicao_do_momento_do_acerto()
    {
        using var ana = await NovoJogadorAsync();
        await Task.Delay(700); // o validador de movimento só deixa andar o que dá correndo
        // Mesma direção para o boneco antes e depois; só a distância muda.
        const float DirecaoParaOBoneco = -56.3f; // de (3, -24) até (0, -22)

        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 3f,
                Z = ValidadorDeMovimento.Nascimento.Z,
                Sequencia = 1,
            }
        );
        // O servidor não devolve a posição ao dono (ADR 0024): confere a dele.
        await PosicaoNoServidor.EsperarAsync(servidor.Falso.Services, ana.Id, p => p.X == 3f); // o movimento foi aceito

        Atacar(ana, direcao: DirecaoParaOBoneco);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve) + Margem.Milliseconds);

        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueAcertou));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Quem_sai_do_alcance_durante_o_preparo_erra_porque_vale_o_momento_do_acerto()
    {
        using var ana = await NovoJogadorAsync();
        await Task.Delay(700); // deixa o validador de movimento permitir 3 m

        Atacar(ana, TipoDeAtaque.Pesado, direcao: 0f); // do nascimento o boneco está na frente
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 3f,
                Z = ValidadorDeMovimento.Nascimento.Z,
                Sequencia = 1,
            }
        );
        await PosicaoNoServidor.EsperarAsync(servidor.Falso.Services, ana.Id, p => p.X == 3f);

        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Pesado) + Margem.Milliseconds);

        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueAcertou));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Quem_chega_ao_alcance_durante_o_preparo_acerta_porque_vale_o_momento_do_acerto()
    {
        using var ana = await NovoJogadorAsync();
        await Task.Delay(700);
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 3f,
                Z = ValidadorDeMovimento.Nascimento.Z,
                Sequencia = 1,
            }
        );
        await PosicaoNoServidor.EsperarAsync(servidor.Falso.Services, ana.Id, p => p.X == 3f);
        await Task.Delay(700);

        // De X = 3 o boneco está fora do alcance e do cone (direção 0).
        Atacar(ana, TipoDeAtaque.Pesado, direcao: 0f);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await Task.Delay(400);
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 0f,
                Z = ValidadorDeMovimento.Nascimento.Z,
                Sequencia = 2,
            }
        );
        await PosicaoNoServidor.EsperarAsync(servidor.Falso.Services, ana.Id, p => p.X == 0f);

        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        Assert.Single(acertou.Acertos);
    }

    [Fact]
    public async Task Pedido_durante_a_acao_e_recusado_com_a_sequencia_do_pedido()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, sequencia: 1);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        Atacar(ana, sequencia: 2); // faltam ~0,8 s: fora da janela da fila de 0,3 s

        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(2, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusa.Codigo);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueIniciado));
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueAcertou));
    }

    [Fact]
    public async Task Pedido_nos_ultimos_0_3_s_da_acao_espera_na_fila_e_comeca_no_fim()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, sequencia: 1);
        var primeiro = DateTime.UtcNow;
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        // Leve: 0,5 s de preparo + 0,3 s de recuperação = 0,8 s. Em 0,6 s faltam 0,2 s.
        await AteAsync(primeiro, 600);
        Atacar(ana, sequencia: 2);

        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        var comecouDepois = DateTime.UtcNow - primeiro;
        var segundo = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.True(
            comecouDepois >= TimeSpan.FromMilliseconds(700),
            $"O pedido da fila começou cedo demais ({comecouDepois.TotalMilliseconds:0} ms)."
        );
        Assert.Equal(ConfigBonecoDeTreino.Vida - 40, Assert.Single(segundo.Acertos).VidaRestante);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueRecusado));
    }

    [Fact]
    public async Task Banco_lento_nao_perde_o_acerto_nem_o_da_fila_e_o_acerto_sai_no_preparo()
    {
        using var ana = await NovoJogadorAsync();
        // O banco demora mais que a recuperação do leve (0,3 s): antes, o golpe da fila virava
        // a ação atual e o acerto do primeiro era descartado.
        servidor.Bonus.AtrasoMs = 400;
        var leiturasAntes = servidor.Bonus.Leituras;
        servidor.Bonus.InicioDasLeituras.Clear();
        servidor.Extra.Consultas.Clear();
        try
        {
            Atacar(ana, sequencia: 1);
            var primeiro = DateTime.UtcNow;
            await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
            await AteAsync(primeiro, 600); // o AtaqueIniciado só sai depois do banco
            Atacar(ana, sequencia: 2); // 0,2 s antes do fim: entra na fila

            var acerto1 = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
                CombateMessageIds.AtaqueAcertou
            );
            // Medido no servidor (da ida ao banco até a procura de alvos), para a carga da máquina
            // não somar a rede e o client: no preparo é ~500 ms; esperando o banco seriam ~900 ms.
            Assert.True(servidor.Bonus.InicioDasLeituras.TryPeek(out var inicio));
            Assert.True(servidor.Extra.Consultas.TryPeek(out var acerto));
            var preparo = RegrasAtaque.PreparoMs(TipoDeAtaque.Leve);
            Assert.True(
                acerto - inicio < preparo + 300,
                $"O acerto saiu tarde ({acerto - inicio} ms depois do pedido)."
            );
            Assert.Equal(
                ConfigBonecoDeTreino.Vida - 20,
                Assert.Single(acerto1.Acertos).VidaRestante
            );

            await TempoRealFixture.EsperarAsync(
                () => ana.Recebidas(CombateMessageIds.AtaqueAcertou) >= 2,
                "o acerto do golpe da fila"
            );
            Assert.Equal(2, ana.Recebidas(CombateMessageIds.AtaqueIniciado));
            Assert.False(ana.Recebeu(CombateMessageIds.AtaqueRecusado));
            // Os atributos ficam em memória: só o primeiro golpe foi ao banco.
            Assert.Equal(1, servidor.Bonus.Leituras - leiturasAntes);
        }
        finally
        {
            servidor.Bonus.AtrasoMs = 0;
        }
    }

    [Fact]
    public async Task Fila_de_um_pedido_so_o_terceiro_e_recusado()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, sequencia: 1);
        var primeiro = DateTime.UtcNow;
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await AteAsync(primeiro, 600);
        Atacar(ana, sequencia: 2); // entra na fila
        await Task.Delay(50);
        Atacar(ana, sequencia: 3); // a fila está ocupada

        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(3, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusa.Codigo);
        // O pedido 2 não se perde: o boneco leva dois golpes.
        await TempoRealFixture.EsperarAsync(
            () => ana.Recebidas(CombateMessageIds.AtaqueAcertou) >= 2,
            "os dois acertos"
        );
        Assert.Equal(ConfigBonecoDeTreino.Vida - 2 * 20, servidor.Boneco.VidaAtual);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueRecusado));
    }

    [Fact]
    public async Task Acerto_leva_tipo_sequencia_e_passo_do_golpe_mesmo_com_o_pesado_na_fila()
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, TipoDeAtaque.Leve, sequencia: 7);
        var inicio = DateTime.UtcNow;
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await AteAsync(inicio, 600);
        Atacar(ana, TipoDeAtaque.Pesado, sequencia: 8); // entra na fila

        var primeiro = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        var segundo = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.Equal(TipoDeAtaque.Leve, primeiro.Tipo);
        Assert.Equal(7, primeiro.Sequencia);
        Assert.Equal(0, primeiro.Passo);
        Assert.Equal(TipoDeAtaque.Pesado, segundo.Tipo);
        Assert.Equal(8, segundo.Sequencia);
        Assert.Equal(0, segundo.Passo);
    }

    [Fact]
    public async Task Pedido_da_fila_sem_stamina_e_recusado_quando_chega_a_vez_dele()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 0);

        Atacar(ana, TipoDeAtaque.Leve, sequencia: 1);
        var primeiro = DateTime.UtcNow;
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await AteAsync(primeiro, 600);
        Atacar(ana, TipoDeAtaque.Pesado, sequencia: 2);

        var recusa = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(2, recusa.Sequencia);
        Assert.Equal(CodigosErroCombate.StaminaInsuficiente, recusa.Codigo);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueIniciado));
    }

    [Fact]
    public async Task Alvos_no_volume_levam_o_golpe_uma_vez_cada_e_o_mesmo_alvo_nao_repete()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Extra.Ativo = true;
        servidor.RepetidoDoBoneco.Ativo = true; // lista o boneco de novo, com o mesmo id

        Atacar(ana);
        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.Equal(2, acertou.Acertos.Length);
        Assert.Single(acertou.Acertos, a => a.AlvoId == ConfigBonecoDeTreino.AlvoId);
        Assert.Single(acertou.Acertos, a => a.AlvoId == -50);
        Assert.Equal(1, servidor.Extra.Aplicacoes);
        Assert.Equal(0, servidor.RepetidoDoBoneco.Aplicacoes);
        Assert.Equal(
            ConfigBonecoDeTreino.Vida - RegrasAtaque.Dano(TipoDeAtaque.Leve, 0),
            servidor.Boneco.VidaAtual
        );
    }

    [Fact]
    public async Task Dois_atacantes_no_mesmo_instante_somam_o_dano_sem_perder_nenhum()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();

        Atacar(ana);
        Atacar(beto);
        // Os dois estão na área, então os dois veem os dois acertos.
        var primeiro = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        var segundo = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );

        Assert.Equal(
            new[] { ana.Id, beto.Id }.Order(),
            new[] { primeiro.AtacanteId, segundo.AtacanteId }.Order()
        );
        Assert.Equal(
            new[] { 140, 160 },
            new[] { primeiro.Acertos[0].VidaRestante, segundo.Acertos[0].VidaRestante }.Order()
        );
        Assert.Equal(ConfigBonecoDeTreino.Vida - 2 * 20, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Ataque_sem_ter_entrado_na_cidade_e_ignorado()
    {
        using var ana = await NovoJogadorAsync(naCidade: false);

        Atacar(ana);
        await Task.Delay(900);

        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueRecusado));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Tipo_de_ataque_fora_do_enum_e_descartado_sem_gastar_nem_acertar(int tipo)
    {
        using var ana = await NovoJogadorAsync();

        Atacar(ana, (TipoDeAtaque)tipo);
        await Task.Delay(900);

        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
        Assert.False(ana.Recebeu(CombateMessageIds.RecursosMudaram));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);

        // E não prendeu o personagem: um ataque válido logo em seguida funciona.
        Atacar(ana, TipoDeAtaque.Leve, sequencia: 2);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
    }

    [Fact]
    public async Task Atacante_que_sai_durante_o_preparo_nao_acerta_ninguem()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();

        Atacar(ana);
        await beto.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        ana.Cliente.Sair();
        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve) + Margem.Milliseconds);

        Assert.False(beto.Recebeu(CombateMessageIds.AtaqueAcertou));
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Boneco_derrubado_nao_e_alvo_e_a_vida_cheia_volta_depois_de_5_segundos()
    {
        using var ana = await NovoJogadorAsync();
        Assert.True(servidor.Boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 170, 1, out _));

        Atacar(ana); // sobram 10 de vida: o golpe leva só o que resta
        var acertou = await ana.Cliente.ReceberAsync<AtaqueAcertou>(
            CombateMessageIds.AtaqueAcertou
        );
        var acerto = Assert.Single(acertou.Acertos);
        Assert.Equal(10, acerto.Dano);
        Assert.Equal(0, acerto.VidaRestante);

        // Derrubado, não há mais alvo: o golpe seguinte erra.
        await Task.Delay(400); // fim da ação
        Atacar(ana, sequencia: 2);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve) + Margem.Milliseconds);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueAcertou));

        servidor.ResetarBoneco();
        Assert.Equal(ConfigBonecoDeTreino.Vida, servidor.Boneco.VidaAtual);
    }

    [Fact]
    public async Task Golpe_que_acerta_publica_GolpeComumAcertou_e_golpe_que_erra_nao_publica()
    {
        using var ana = await NovoJogadorAsync();
        var eventos = servidor.Falso.Services.GetRequiredService<EventosDoServidor>();
        var acertos = new List<GolpeComumAcertou>();
        eventos.Assinar<GolpeComumAcertou>(e =>
        {
            if (e.PersonagemId == ana.Id)
            {
                lock (acertos)
                {
                    acertos.Add(e);
                }
            }
        });

        // 1. Golpe na direção do boneco (0 graus): acerta e publica GolpeComumAcertou
        Atacar(ana, TipoDeAtaque.Leve, sequencia: 1, direcao: 0f);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await ana.Cliente.ReceberAsync<AtaqueAcertou>(CombateMessageIds.AtaqueAcertou);
        await TempoRealFixture.EsperarAsync(
            () =>
            {
                lock (acertos)
                    return acertos.Count == 1;
            },
            "GolpeComumAcertou"
        );

        lock (acertos)
        {
            Assert.Single(acertos);
            Assert.Equal(ana.Id, acertos[0].PersonagemId);
            Assert.Contains(ConfigBonecoDeTreino.AlvoId, acertos[0].AlvoIds);
        }

        // Espera a recuperação do golpe leve terminar antes do próximo
        await Task.Delay(400);

        // 2. Golpe de costas para o boneco (180 graus): erra (não envia AtaqueAcertou) e não publica
        await VirarDeCostasAsync(ana, sequencia: 2);
        Atacar(ana, TipoDeAtaque.Leve, sequencia: 2, direcao: 180f);
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        await Task.Delay(RegrasAtaque.PreparoMs(TipoDeAtaque.Leve) + Margem.Milliseconds);

        Assert.Equal(1, ana.Recebidas(CombateMessageIds.AtaqueAcertou));
        lock (acertos)
        {
            Assert.Single(acertos);
        }
    }
}
