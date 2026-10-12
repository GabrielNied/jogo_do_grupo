using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// Reação ao acerto (#577) e guarda segurada (#579) de ponta a ponta: client UDP de verdade, o
/// golpe de inimigo publicado direto (o círculo é decisão de Mobs) e o Mundo de verdade.
/// </summary>
public class ReacaoEGuardaRedeTestes(ServidorDeAtaque servidor) : IClassFixture<ServidorDeAtaque>
{
    private async Task<Jogador> NovoJogadorAsync()
    {
        servidor.Bonus.Equipamentos = new AtributosCombate();
        servidor.Bonus.Efeitos = new AtributosCombate();

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
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return jogador;
    }

    private void Golpear(long personagemId, int dano) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, personagemId, dano, "cidade"));

    private static void Guardar(Jogador j, bool segurando) =>
        j.Cliente.Enviar(
            CombateMessageIds.IntencaoDeGuarda,
            new IntencaoDeGuarda { Segurando = segurando }
        );

    private static Task<GuardaMudou> GuardaAsync(Jogador j) =>
        j.Cliente.ReceberAsync<GuardaMudou>(CombateMessageIds.GuardaMudou);

    private static Task<RecursosMudaram> RecursosAsync(Jogador j) =>
        j.Cliente.ReceberAsync<RecursosMudaram>(CombateMessageIds.RecursosMudaram);

    private static async Task<RecursosResponse> ConsultarAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasCombate.RecursosDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    private async Task DefinirVigorAsync(Jogador j, int vigor)
    {
        await ConsultarAsync(j); // Cria a linha.
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id);
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        var agora = servidor.Relogio.GetUtcNow().UtcDateTime;
        r.Vigor = vigor;
        r.UltimoGastoVigorEm = agora;
        r.VigorContadoAte = agora;
        await db.SaveChangesAsync();
        memoria.Descartar(j.Id);
    }

    [Fact]
    public async Task Depois_do_acerto_esquiva_e_ataque_sao_recusados_por_300_ms()
    {
        using var ana = await NovoJogadorAsync();

        Golpear(ana.Id, 10);
        var interrompida = await ana.Cliente.ReceberAsync<AcaoInterrompida>(
            CombateMessageIds.AcaoInterrompida
        );
        Assert.True(interrompida.Jogador);
        Assert.Equal(ana.Id, interrompida.AlvoId);
        Assert.Equal(RegrasReacao.AtordoamentoNoAcertoMs, interrompida.DuracaoMs);

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 1 }
        );
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Leve, Sequencia = 2 }
        );

        var esquiva = await ana.Cliente.ReceberAsync<EsquivaRecusada>(
            CombateMessageIds.EsquivaRecusada
        );
        var ataque = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, esquiva.Codigo);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, ataque.Codigo);
    }

    [Fact]
    public async Task Guarda_com_vigor_anula_o_golpe_e_desconta_o_vigor()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        await DefinirVigorAsync(ana, 100);
        var vida = (await ConsultarAsync(ana)).VidaAtual;

        Guardar(ana, true);
        var sobe = await GuardaAsync(beto); // Quem está na área também vê a postura.
        Assert.Equal(ana.Id, sobe.PersonagemId);
        Assert.True(sobe.Segurando);

        Golpear(ana.Id, 30);

        var recursos = await RecursosAsync(ana);
        Assert.Equal(100 - 30 * RegrasGuarda.VigorPorDanoBloqueado, recursos.VigorAtual);
        Assert.Equal(vida, recursos.VidaAtual);
        var bloqueado = await ana.Cliente.ReceberAsync<GolpeBloqueado>(
            CombateMessageIds.GolpeBloqueado
        );
        Assert.Equal(30, bloqueado.VigorGasto);
        Assert.False(ana.Recebeu(CombateMessageIds.AcaoInterrompida)); // Bloqueado não interrompe.
        Assert.Equal(vida, (await ConsultarAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Guarda_sem_vigor_quebra_toma_o_dano_e_fica_300_ms_sem_agir()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirVigorAsync(ana, 10);
        var vida = (await ConsultarAsync(ana)).VidaAtual;
        Guardar(ana, true);
        Assert.True((await GuardaAsync(ana)).Segurando);

        Golpear(ana.Id, 30);

        var quebrou = await GuardaAsync(ana);
        Assert.False(quebrou.Segurando);
        Assert.True(quebrou.Quebrou);
        await ana.Cliente.ReceberAsync<AcaoInterrompida>(CombateMessageIds.AcaoInterrompida);
        var final = await ConsultarAsync(ana);
        Assert.Equal(vida - 30, final.VidaAtual);
        Assert.True(final.VigorAtual <= 1); // Foi a zero; no máximo a regeneração de 1 tick.

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 1 }
        );
        var recusa = await ana.Cliente.ReceberAsync<EsquivaRecusada>(
            CombateMessageIds.EsquivaRecusada
        );
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusa.Codigo);
    }

    [Fact]
    public async Task Segurar_a_guarda_no_preparo_do_golpe_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Leve, Sequencia = 1 }
        );

        Guardar(ana, true);

        var resposta = await GuardaAsync(ana);
        Assert.False(resposta.Segurando);
        Assert.False(resposta.Quebrou);
    }

    [Fact]
    public async Task Guarda_pede_ao_mundo_o_teto_de_40_por_cento_e_solta_ao_largar()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();

        Guardar(ana, true);
        Assert.True((await GuardaAsync(ana)).Segurando);
        Assert.True((await GuardaAsync(beto)).Segurando);
        Assert.Contains(
            servidor.Limite.Pedidos,
            p => p.PersonagemId == ana.Id && p.Fator == RegrasGuarda.VelocidadeComGuarda
        );

        // Quem soltou não recebe o próprio eco; quem está na área vê a guarda descer.
        Guardar(ana, false);
        Assert.False((await GuardaAsync(beto)).Segurando);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.GuardaMudou));
        Assert.Contains(servidor.Limite.Pedidos, p => p.PersonagemId == ana.Id && p.Fator == 1.0);
    }
}
