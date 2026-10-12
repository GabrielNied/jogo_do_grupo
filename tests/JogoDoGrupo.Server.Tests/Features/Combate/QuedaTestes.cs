using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>Posições que o teste controla (o Mundo de verdade não é usado aqui).</summary>
public sealed class PosicoesFalsas : IPosicoesDoMundo
{
    private readonly ConcurrentDictionary<long, PosicaoNoMundo> _posicoes = new();

    public void Colocar(long personagemId, string area, float x, float z) =>
        _posicoes[personagemId] = new PosicaoNoMundo(area, x, 0f, z, 0f);

    /// <summary>Muda só o lugar, mantendo a área.</summary>
    public void Mover(long personagemId, float x, float z) =>
        Colocar(personagemId, _posicoes[personagemId].Area, x, z);

    public bool TentarObter(long personagemId, out PosicaoNoMundo posicao) =>
        _posicoes.TryGetValue(personagemId, out posicao);

    public IReadOnlyList<long> PersonagensNaArea(string area) =>
        _posicoes.Where(p => p.Value.Area == area).Select(p => p.Key).ToArray();
}

/// <summary>
/// Servidor de teste do estado caído: banco real, posições falsas e tempos curtos (3 s caído,
/// 0,6 s para levantar, 0,7 s até poder voltar) para os testes não esperarem 45 s. Os tempos
/// reais estão em <c>ControleDeQuedaTestes</c> e <c>RegrasQuedaTestes</c>.
/// </summary>
public sealed class ServidorDeQueda : IDisposable
{
    public static readonly ConfigQueda Config = new()
    {
        TempoCaidoMs = 3_000,
        TempoParaLevantarMs = 600,
        MinimoParaVoltarMs = 700,
        IntervaloDeConferenciaMs = 50,
    };

    private readonly ConcurrentQueue<object> _eventos = new();

    public ServidorDeTeste Real { get; } = new();
    public PosicoesFalsas Posicoes { get; } = new();
    public WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorDeQueda()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IPosicoesDoMundo>();
                s.AddSingleton<IPosicoesDoMundo>(Posicoes);
                s.RemoveAll<ConfigQueda>();
                s.AddSingleton(Config);
            })
        );
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var eventos = Falso.Services.GetRequiredService<EventosDoServidor>();
        eventos.Assinar<PersonagemCaiu>(_eventos.Enqueue);
        eventos.Assinar<PersonagemLevantou>(_eventos.Enqueue);
        eventos.Assinar<PersonagemVoltouParaACidade>(_eventos.Enqueue);
    }

    public IEstadoDeQueda Queda => Falso.Services.GetRequiredService<IEstadoDeQueda>();

    public int Eventos<T>(long personagemId)
        where T : class =>
        _eventos
            .OfType<T>()
            .Count(e =>
                e switch
                {
                    PersonagemCaiu c => c.PersonagemId == personagemId,
                    PersonagemLevantou l => l.PersonagemId == personagemId,
                    PersonagemVoltouParaACidade v => v.PersonagemId == personagemId,
                    _ => false,
                }
            );

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

/// <summary>
/// Integração do estado caído: client UDP de verdade contra o servidor, com banco SQLite. Os
/// tempos são os curtos de <see cref="ServidorDeQueda.Config"/>.
/// </summary>
public class QuedaTestes(ServidorDeQueda servidor) : IClassFixture<ServidorDeQueda>
{
    private static readonly TimeSpan Silencio = TimeSpan.FromMilliseconds(400);

    // Uma área por teste: o estado de queda vai a todos da área, e testes de personagens
    // diferentes não podem se ouvir.
    private readonly string _area = "area-" + Guid.NewGuid().ToString("N");

    private async Task<Jogador> NovoJogadorAsync(float x = 0f, float z = 0f)
    {
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
        servidor.Posicoes.Colocar(personagem.Id, _area, x, z);
        return new Jogador(http, cliente, personagem.Id);
    }

    private void Golpear(Jogador j, int dano) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, j.Id, dano, _area));

    private async Task<Jogador> CaidoAsync(float x = 0f, float z = 0f)
    {
        var j = await NovoJogadorAsync(x, z);
        await DerrubarAsync(j);
        return j;
    }

    private async Task DerrubarAsync(Jogador j)
    {
        Golpear(j, 100_000);
        var estado = await EstadoAsync(j, FaseDeQueda.Caido);
        Assert.Equal(j.Id, estado.PersonagemId);
    }

    /// <summary>Espera o próximo <c>EstadoDeQueda</c> da fase e do personagem pedidos (descarta os outros).</summary>
    private static async Task<EstadoDeQueda> EstadoAsync(
        Jogador j,
        FaseDeQueda fase,
        long? de = null
    )
    {
        while (true)
        {
            var estado = await j.Cliente.ReceberAsync<EstadoDeQueda>(
                CombateMessageIds.EstadoDeQueda
            );
            if (estado.Estado == fase && estado.PersonagemId == (de ?? j.Id))
            {
                return estado;
            }
        }
    }

    private static Task<AcaoRecusada> RecusadaAsync(Jogador j) =>
        j.Cliente.ReceberAsync<AcaoRecusada>(CombateMessageIds.AcaoRecusada);

    private static void Levantar(Jogador ajudante, Jogador alvo) =>
        ajudante.Cliente.Enviar(
            CombateMessageIds.ComecarALevantar,
            new ComecarALevantar { AlvoId = alvo.Id }
        );

    private static void Voltar(Jogador j) =>
        j.Cliente.Enviar(CombateMessageIds.VoltarParaACidade, new VoltarParaACidade());

    private static async Task<RecursosResponse> RecursosAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasCombate.RecursosDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    private static async Task<int[]> InventarioAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasItens.InventarioDe(j.Id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var inventario = await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resposta);
        return inventario.Entradas.Select(e => e.ItemId).ToArray();
    }

    private static async Task<int[]> GanharItensAsync(Jogador j)
    {
        var resposta = await j.Http.PostAsync(RotasItens.BauDe(j.Id), null);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var itens = await InventarioAsync(j);
        Assert.NotEmpty(itens);
        return itens;
    }

    private async Task GastarVigorEEnergiaAsync(Jogador j)
    {
        await RecursosAsync(j); // Cria a linha.
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id); // O banco precisa ter o que a memória já mudou.
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        var agora = DateTime.UtcNow;
        r.Vigor = 10;
        r.Energia = 10;
        r.UltimoGastoVigorEm = agora;
        r.UltimoGastoEnergiaEm = agora;
        r.VigorContadoAte = agora;
        r.EnergiaContadaAte = agora;
        await db.SaveChangesAsync();
        memoria.Descartar(j.Id); // E a memória precisa reler o que o teste mudou.
    }

    private static async Task SemMensagemAsync(Jogador j, ushort id, int antes)
    {
        await Task.Delay(Silencio);
        Assert.Equal(antes, j.Recebidas(id));
    }

    // ── Cair ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Vida_zerada_leva_a_caido_e_avisa_o_dono_e_a_area()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);

        await DerrubarAsync(ana);

        var paraBeto = await EstadoAsync(beto, FaseDeQueda.Caido, ana.Id);
        Assert.Equal(ana.Id, paraBeto.PersonagemId);
        Assert.Equal(0L, paraBeto.AjudanteId);
        Assert.InRange(paraBeto.SegundosRestantes, 1, 3);
        Assert.Equal(0, (await RecursosAsync(ana)).VidaAtual);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
        Assert.False(servidor.Queda.EstaCaido(beto.Id));
        Assert.Equal(1, servidor.Eventos<PersonagemCaiu>(ana.Id));
    }

    [Fact]
    public async Task Caido_nao_recebe_dano_nem_cai_de_novo()
    {
        using var ana = await CaidoAsync();
        var avisosDeRecursos = ana.Recebidas(CombateMessageIds.RecursosMudaram);

        Golpear(ana, 50);
        Golpear(ana, 50);

        await SemMensagemAsync(ana, CombateMessageIds.RecursosMudaram, avisosDeRecursos);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.EstadoDeQueda));
        Assert.Equal(1, servidor.Eventos<PersonagemCaiu>(ana.Id));
    }

    [Fact]
    public async Task Caido_nao_ataca_nem_esquiva()
    {
        using var ana = await CaidoAsync();

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque
            {
                Tipo = TipoDeAtaque.Leve,
                DirecaoY = 0f,
                Sequencia = 3,
            }
        );
        var ataque = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(CodigosErroCombate.VoceEstaCaido, ataque.Codigo);

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 4 }
        );
        var esquiva = await ana.Cliente.ReceberAsync<EsquivaRecusada>(
            CombateMessageIds.EsquivaRecusada
        );
        Assert.Equal(CodigosErroCombate.VoceEstaCaido, esquiva.Codigo);
        Assert.False(ana.Recebeu(CombateMessageIds.AtaqueIniciado));
        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaIniciada));
    }

    // ── Levantar ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Aliado_levanta_o_caido_com_20_por_cento_da_vida_e_sem_perder_itens()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync(1.5f, 0f);
        var itens = await GanharItensAsync(ana);
        await DerrubarAsync(ana);

        Levantar(beto, ana);

        var levantando = await EstadoAsync(ana, FaseDeQueda.Levantando);
        Assert.Equal(beto.Id, levantando.AjudanteId);
        var paraBeto = await EstadoAsync(beto, FaseDeQueda.Levantando, ana.Id);
        Assert.Equal(ana.Id, paraBeto.PersonagemId);
        var vivo = await EstadoAsync(ana, FaseDeQueda.Vivo);
        Assert.Equal(beto.Id, vivo.AjudanteId);

        var vidaMaxima = (await RecursosAsync(ana)).VidaMaxima;
        Assert.Equal(RegrasQueda.VidaAoLevantar(vidaMaxima), (await RecursosAsync(ana)).VidaAtual);
        Assert.False(servidor.Queda.EstaCaido(ana.Id));
        Assert.Equal(1, servidor.Eventos<PersonagemLevantou>(ana.Id));
        Assert.Equal(itens, await InventarioAsync(ana));
    }

    [Fact]
    public async Task Depois_de_levantar_o_fim_do_tempo_antigo_nao_traz_um_segundo_resultado()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Vivo);

        await Task.Delay(ServidorDeQueda.Config.TempoCaidoMs + 400);

        Assert.Equal(0, servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id));
        Assert.Equal(1, servidor.Eventos<PersonagemLevantou>(ana.Id));
        var vida = await RecursosAsync(ana);
        Assert.Equal(RegrasQueda.VidaAoLevantar(vida.VidaMaxima), vida.VidaAtual);
    }

    [Fact]
    public async Task Levantado_pode_cair_de_novo()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Vivo);

        await DerrubarAsync(ana);

        Assert.True(servidor.Queda.EstaCaido(ana.Id));
        Assert.Equal(2, servidor.Eventos<PersonagemCaiu>(ana.Id));
    }

    [Fact]
    public async Task Levantar_a_exatamente_2_metros_vale()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(2f, 0f);

        Levantar(beto, ana);

        await EstadoAsync(ana, FaseDeQueda.Levantando);
        await EstadoAsync(ana, FaseDeQueda.Vivo);
    }

    [Fact]
    public async Task Dois_ajudantes_ao_mesmo_tempo_levantam_uma_vez_so()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        using var caio = await NovoJogadorAsync(0f, 1f);

        await Task.WhenAll(
            Task.Run(() => Levantar(beto, ana)),
            Task.Run(() => Levantar(caio, ana))
        );

        await EstadoAsync(ana, FaseDeQueda.Vivo);
        await Task.Delay(Silencio);
        var recusas =
            beto.Recebidas(CombateMessageIds.AcaoRecusada)
            + caio.Recebidas(CombateMessageIds.AcaoRecusada);
        Assert.Equal(1, recusas);
        Assert.Equal(1, servidor.Eventos<PersonagemLevantou>(ana.Id));
        var vida = await RecursosAsync(ana);
        Assert.Equal(RegrasQueda.VidaAoLevantar(vida.VidaMaxima), vida.VidaAtual); // não curou em dobro
    }

    // ── Interrupções ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dano_no_ajudante_interrompe_e_zera_o_progresso()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        Golpear(beto, 10);

        var volta = await EstadoAsync(ana, FaseDeQueda.Caido);
        Assert.Equal(0L, volta.AjudanteId);
        Assert.Equal(0, volta.ProgressoMs);
        await Task.Delay(ServidorDeQueda.Config.TempoParaLevantarMs + 300);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
        Assert.Equal(0, servidor.Eventos<PersonagemLevantou>(ana.Id));
        Assert.True((await RecursosAsync(beto)).VidaAtual < (await RecursosAsync(beto)).VidaMaxima);
    }

    [Fact]
    public async Task Ajudante_que_cai_no_meio_interrompe_a_interacao()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        Golpear(beto, 100_000);

        await EstadoAsync(ana, FaseDeQueda.Caido);
        Assert.True(servidor.Queda.EstaCaido(beto.Id));
        await Task.Delay(ServidorDeQueda.Config.TempoParaLevantarMs + 300);
        Assert.Equal(0, servidor.Eventos<PersonagemLevantou>(ana.Id));
    }

    [Fact]
    public async Task Ajudante_que_solta_a_interacao_cancela()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        beto.Cliente.Enviar(CombateMessageIds.CancelarLevantar, new CancelarLevantar());

        await EstadoAsync(ana, FaseDeQueda.Caido);
        await Task.Delay(ServidorDeQueda.Config.TempoParaLevantarMs + 300);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
    }

    [Fact]
    public async Task Ajudante_que_se_afasta_mais_de_2_metros_interrompe()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        servidor.Posicoes.Mover(beto.Id, 2.5f, 0f);

        await EstadoAsync(ana, FaseDeQueda.Caido);
        await Task.Delay(ServidorDeQueda.Config.TempoParaLevantarMs + 300);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
    }

    [Fact]
    public async Task Ajudante_que_ataca_ou_esquiva_interrompe()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        beto.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque
            {
                Tipo = TipoDeAtaque.Leve,
                DirecaoY = 0f,
                Sequencia = 1,
            }
        );
        await EstadoAsync(ana, FaseDeQueda.Caido);

        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);
        beto.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 2 }
        );
        await EstadoAsync(ana, FaseDeQueda.Caido);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
    }

    // ── Recusas ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Levantar_quem_esta_vivo_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);

        Levantar(beto, ana);

        Assert.Equal(CodigosErroCombate.AlvoNaoEstaCaido, (await RecusadaAsync(beto)).Codigo);
        Assert.Equal(0, ana.Recebidas(CombateMessageIds.EstadoDeQueda));
    }

    [Fact]
    public async Task Levantar_a_mais_de_2_metros_e_recusado_e_nada_muda()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(2.1f, 0f);

        Levantar(beto, ana);

        Assert.Equal(CodigosErroCombate.LongeDemais, (await RecusadaAsync(beto)).Codigo);
        await SemMensagemAsync(ana, CombateMessageIds.EstadoDeQueda, 1);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
    }

    [Fact]
    public async Task Levantar_alguem_que_ja_esta_sendo_levantado_e_recusado()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        using var caio = await NovoJogadorAsync(0f, 1f);
        Levantar(beto, ana);
        await EstadoAsync(ana, FaseDeQueda.Levantando);

        Levantar(caio, ana);

        Assert.Equal(CodigosErroCombate.JaEstaSendoLevantado, (await RecusadaAsync(caio)).Codigo);
        var vivo = await EstadoAsync(ana, FaseDeQueda.Vivo);
        Assert.Equal(beto.Id, vivo.AjudanteId); // o primeiro seguiu até o fim
    }

    [Fact]
    public async Task Caido_nao_levanta_ninguem()
    {
        using var ana = await CaidoAsync();
        using var beto = await CaidoAsync(1f, 0f);

        Levantar(beto, ana);

        Assert.Equal(CodigosErroCombate.VoceEstaCaido, (await RecusadaAsync(beto)).Codigo);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
    }

    [Fact]
    public async Task Levantar_quem_acabou_de_voltar_a_cidade_e_recusado()
    {
        using var ana = await CaidoAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        await EstadoAsync(ana, FaseDeQueda.Retorno); // fim do tempo

        Levantar(beto, ana);

        Assert.Equal(CodigosErroCombate.AlvoNaoEstaCaido, (await RecusadaAsync(beto)).Codigo);
    }

    [Fact]
    public async Task Voltar_sem_estar_caido_e_recusado()
    {
        using var ana = await NovoJogadorAsync();

        Voltar(ana);

        Assert.Equal(CodigosErroCombate.NaoEstaCaido, (await RecusadaAsync(ana)).Codigo);
    }

    [Fact]
    public async Task Voltar_antes_do_minimo_e_recusado_e_continua_caido()
    {
        using var ana = await CaidoAsync();

        Voltar(ana);

        Assert.Equal(CodigosErroCombate.AindaNaoPodeVoltar, (await RecusadaAsync(ana)).Codigo);
        Assert.True(servidor.Queda.EstaCaido(ana.Id));
        Assert.Equal(0, servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id));
    }

    // ── Voltar à cidade ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Depois_do_minimo_o_caido_volta_com_tudo_cheio_sem_perder_itens()
    {
        using var ana = await NovoJogadorAsync();
        var itens = await GanharItensAsync(ana);
        await GastarVigorEEnergiaAsync(ana);
        await DerrubarAsync(ana);
        await Task.Delay(ServidorDeQueda.Config.MinimoParaVoltarMs + 100);

        Voltar(ana);

        var retorno = await EstadoAsync(ana, FaseDeQueda.Retorno);
        Assert.Equal(ana.Id, retorno.PersonagemId);
        var recursos = await RecursosAsync(ana);
        Assert.Equal(recursos.VidaMaxima, recursos.VidaAtual);
        Assert.Equal(recursos.VigorMaximo, recursos.VigorAtual);
        Assert.Equal(recursos.EnergiaMaxima, recursos.EnergiaAtual);
        Assert.False(servidor.Queda.EstaCaido(ana.Id));
        Assert.Equal(1, servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id));
        Assert.Equal(itens, await InventarioAsync(ana));
    }

    [Fact]
    public async Task Fim_do_tempo_volta_sozinho_uma_vez_so()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync(1f, 0f);
        await DerrubarAsync(ana);

        await EstadoAsync(ana, FaseDeQueda.Retorno);
        await EstadoAsync(beto, FaseDeQueda.Retorno, ana.Id); // quem está na área também vê

        await Task.Delay(Silencio);
        var recursos = await RecursosAsync(ana);
        Assert.Equal(recursos.VidaMaxima, recursos.VidaAtual);
        Assert.Equal(1, servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id));
        Assert.Equal(0, servidor.Eventos<PersonagemLevantou>(ana.Id));
        Assert.False(servidor.Queda.EstaCaido(ana.Id));
    }

    [Fact]
    public async Task Pedir_para_voltar_depois_do_fim_do_tempo_e_recusado()
    {
        using var ana = await CaidoAsync();
        await EstadoAsync(ana, FaseDeQueda.Retorno);

        Voltar(ana);

        Assert.Equal(CodigosErroCombate.NaoEstaCaido, (await RecusadaAsync(ana)).Codigo);
        await Task.Delay(Silencio);
        Assert.Equal(1, servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id));
    }

    // ── Concorrência ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fim_do_tempo_e_levantar_quase_juntos_dao_uma_saida_so()
    {
        var config = ServidorDeQueda.Config;
        for (var rodada = 0; rodada < 3; rodada++)
        {
            using var ana = await NovoJogadorAsync();
            using var beto = await NovoJogadorAsync(1f, 0f);
            var relogio = Stopwatch.StartNew();
            await DerrubarAsync(ana);

            // Começa de modo que os 0,6 s acabem quase junto com os 3 s.
            var espera =
                config.TempoCaidoMs - config.TempoParaLevantarMs - relogio.ElapsedMilliseconds;
            await Task.Delay((int)Math.Max(0, espera));
            Levantar(beto, ana);

            await Task.Delay(config.TempoCaidoMs + 400);
            var saidas =
                servidor.Eventos<PersonagemLevantou>(ana.Id)
                + servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id);
            Assert.Equal(1, saidas);
            Assert.Equal(
                1,
                ana.Cliente.IdsRecebidos.Count(i => i == CombateMessageIds.RecursosMudaram) - 1 // o RecursosMudaram do golpe que derrubou
            );
            Assert.False(servidor.Queda.EstaCaido(ana.Id));
        }
    }

    [Fact]
    public async Task Voltar_pedido_junto_com_o_fim_da_interacao_da_uma_saida_so()
    {
        for (var rodada = 0; rodada < 3; rodada++)
        {
            using var ana = await NovoJogadorAsync();
            using var beto = await NovoJogadorAsync(1f, 0f);
            await DerrubarAsync(ana);
            await Task.Delay(ServidorDeQueda.Config.MinimoParaVoltarMs + 50);
            Levantar(beto, ana);
            await EstadoAsync(ana, FaseDeQueda.Levantando);
            await Task.Delay(ServidorDeQueda.Config.TempoParaLevantarMs - 10);

            Voltar(ana);

            await Task.Delay(ServidorDeQueda.Config.TempoCaidoMs + 400);
            var saidas =
                servidor.Eventos<PersonagemLevantou>(ana.Id)
                + servidor.Eventos<PersonagemVoltouParaACidade>(ana.Id);
            Assert.Equal(1, saidas);
            Assert.False(servidor.Queda.EstaCaido(ana.Id));
        }
    }
}
