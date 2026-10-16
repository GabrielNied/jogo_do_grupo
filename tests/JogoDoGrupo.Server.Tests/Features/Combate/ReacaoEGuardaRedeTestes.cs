using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
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
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        servidor.Bonus.Efeitos = new AtributosCombate();
        servidor.RelogioCombate.Soltar(); // cada teste decide se controla o tempo

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

    private void Golpear(
        long personagemId,
        int dano,
        TipoDeAtaqueDeInimigo tipo = TipoDeAtaqueDeInimigo.Golpe
    ) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, personagemId, dano, "cidade", tipo));

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

    private async Task DefinirStaminaAsync(Jogador j, int stamina)
    {
        await ConsultarAsync(j); // Cria a linha.
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id);
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        var agora = servidor.Relogio.GetUtcNow().UtcDateTime;
        r.Stamina = stamina;
        r.UltimoGastoStaminaEm = agora;
        r.StaminaContadaAte = agora;
        await db.SaveChangesAsync();
        memoria.Descartar(j.Id);
    }

    [Fact]
    public async Task Depois_do_acerto_esquiva_e_ataque_sao_recusados_por_300_ms()
    {
        using var ana = await NovoJogadorAsync();

        Golpear(ana.Id, 10, TipoDeAtaqueDeInimigo.Investida);
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
    public async Task Golpe_comum_de_inimigo_nao_interrompe_nem_atordoa_o_jogador()
    {
        using var ana = await NovoJogadorAsync();

        Golpear(ana.Id, 10, TipoDeAtaqueDeInimigo.Golpe);
        var recursos = await RecursosAsync(ana);
        Assert.True(recursos.VidaAtual > 0);

        // Não recebe AcaoInterrompida.
        Assert.False(ana.Recebeu(CombateMessageIds.AcaoInterrompida));

        // Esquiva imediata é aceita, sem recusa por ação em andamento.
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 1 }
        );
        var esquiva = await ana.Cliente.ReceberAsync<EsquivaIniciada>(
            CombateMessageIds.EsquivaIniciada
        );
        Assert.Equal(ana.Id, esquiva.PersonagemId);
        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaRecusada));
    }

    [Fact]
    public async Task Guarda_com_stamina_anula_o_golpe_e_desconta_o_stamina()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        await DefinirStaminaAsync(ana, 100);
        var vida = (await ConsultarAsync(ana)).VidaAtual;

        Guardar(ana, true);
        var sobe = await GuardaAsync(beto); // Quem está na área também vê a postura.
        Assert.Equal(ana.Id, sobe.PersonagemId);
        Assert.True(sobe.Segurando);
        var aoGuardar = await RecursosAsync(ana); // A previsão do client precisa saber da guarda.
        Assert.Equal(100, aoGuardar.StaminaAtual);
        Assert.Equal(RegrasRecursos.RegeneracaoStaminaPorSegundo, aoGuardar.RegeneracaoStamina);

        Golpear(ana.Id, 30);

        var recursos = await RecursosAsync(ana);
        Assert.Equal(100 - 30 * RegrasGuarda.StaminaPorDanoBloqueado, recursos.StaminaAtual);
        Assert.Equal(vida, recursos.VidaAtual);
        var bloqueado = await ana.Cliente.ReceberAsync<GolpeBloqueado>(
            CombateMessageIds.GolpeBloqueado
        );
        Assert.Equal(30, bloqueado.StaminaGasto);
        Assert.False(ana.Recebeu(CombateMessageIds.AcaoInterrompida)); // Bloqueado não interrompe.
        Assert.Equal(vida, (await ConsultarAsync(ana)).VidaAtual);
    }

    private static Task<HttpResponseMessage> TrocarConjuntoPeloBotaoAsync(
        Jogador j,
        int conjunto
    ) =>
        ServidorDeTeste.PostJsonAsync(
            j.Http,
            RegrasEquipamentos.RotaDe(j.Id) + "/conjunto",
            new PedidoConjunto { Conjunto = conjunto }
        );

    [Fact]
    public async Task Botao_de_conjunto_durante_o_preparo_do_golpe_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Pesado, Sequencia = 1 }
        );
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);

        var resposta = await TrocarConjuntoPeloBotaoAsync(ana, 2);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains(
            CodigosErroItens.TrocaDeArmaBloqueada,
            await resposta.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task Botao_de_conjunto_parado_troca_o_conjunto()
    {
        // O tempo dos 0,4 s é testado com relógio controlado em TrocaDeArmaTestes.
        using var ana = await NovoJogadorAsync();

        var troca = await TrocarConjuntoPeloBotaoAsync(ana, 2);

        troca.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Guarda_sem_stamina_quebra_toma_o_dano_e_fica_300_ms_sem_agir()
    {
        using var ana = await NovoJogadorAsync();
        servidor.RelogioCombate.Controlar(); // os 300 ms só passam quando o teste manda
        await DefinirStaminaAsync(ana, 10);
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
        Assert.True(final.StaminaAtual <= 1); // Foi a zero; no máximo a regeneração de 1 tick.

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 1 }
        );
        var recusa = await ana.Cliente.ReceberAsync<EsquivaRecusada>(
            CombateMessageIds.EsquivaRecusada
        );
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusa.Codigo);

        // Passados os 300 ms o personagem volta a agir: a esquiva agora só falha por falta de stamina.
        servidor.RelogioCombate.Avancar(RegrasReacao.AtordoamentoNoAcertoMs);
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = 0f, Sequencia = 2 }
        );
        var semStamina = await ana.Cliente.ReceberAsync<EsquivaRecusada>(
            CombateMessageIds.EsquivaRecusada
        );
        Assert.NotEqual(CodigosErroCombate.AcaoEmAndamento, semStamina.Codigo);
    }

    [Fact]
    public async Task Segurar_a_guarda_no_preparo_do_golpe_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        // Pesado: o preparo de 1 s dá folga sob carga (o leve, de 0,5 s, perdia a janela). A guarda
        // só é pedida depois da mensagem de preparo, então cai dentro dele.
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Pesado, Sequencia = 1 }
        );
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);

        Guardar(ana, true);

        var resposta = await GuardaAsync(ana);
        Assert.False(resposta.Segurando);
        Assert.False(resposta.Quebrou);
    }

    [Fact]
    public async Task Segurar_a_guarda_durante_atordoamento_e_recusado_com_VoceEstaAtordoado()
    {
        using var ana = await NovoJogadorAsync();
        var efeitos = servidor.Falso.Services.GetRequiredService<GerenciadorDeEfeitosDeStatus>();
        efeitos.Aplicar(ana.Id, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 5.0);

        Guardar(ana, true);

        var resposta = await GuardaAsync(ana);
        Assert.False(resposta.Segurando);
        Assert.False(resposta.Quebrou);

        var recusa = await ana.Cliente.ReceberAsync<AcaoRecusada>(CombateMessageIds.AcaoRecusada);
        Assert.Equal(CodigosErroCombate.VoceEstaAtordoado, recusa.Codigo);
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

    [Fact]
    public async Task Segurar_e_soltar_a_guarda_mandam_os_recursos_ao_dono()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirStaminaAsync(ana, 60);

        Guardar(ana, true);
        var segurando = await RecursosAsync(ana);
        Guardar(ana, false);
        var solto = await RecursosAsync(ana);

        Assert.Equal(60, segurando.StaminaAtual);
        Assert.Equal(60, solto.StaminaAtual); // A espera de 2 s recomeçou: nada regenerou.
        Assert.Equal(RegrasRecursos.StaminaBase, solto.StaminaMaxima);
    }
}
