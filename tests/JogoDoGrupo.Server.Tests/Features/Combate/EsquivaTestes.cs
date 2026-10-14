using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// Integração da esquiva: client UDP de verdade contra o servidor, com banco SQLite e o Mundo
/// de verdade. A conta fina da janela de deslocamento (5 m, 0,6 s) está nos testes do Mundo,
/// com relógio controlado; aqui o tempo é o real.
/// </summary>
public class EsquivaTestes(ServidorDeAtaque servidor) : IClassFixture<ServidorDeAtaque>
{
    private async Task<Jogador> NovoJogadorAsync(bool naCidade = true)
    {
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
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
        if (naCidade)
        {
            cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
            await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        }

        return jogador;
    }

    private static void Esquivar(Jogador j, int sequencia = 1, float direcao = 0f) =>
        j.Cliente.Enviar(
            CombateMessageIds.IntencaoDeEsquiva,
            new IntencaoDeEsquiva { DirecaoY = direcao, Sequencia = sequencia }
        );

    private static Task<EsquivaIniciada> IniciadaAsync(Jogador j) =>
        j.Cliente.ReceberAsync<EsquivaIniciada>(CombateMessageIds.EsquivaIniciada);

    private static Task<EsquivaRecusada> RecusadaAsync(Jogador j) =>
        j.Cliente.ReceberAsync<EsquivaRecusada>(CombateMessageIds.EsquivaRecusada);

    private static async Task<int> StaminaAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasCombate.RecursosDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta)).StaminaAtual;
    }

    /// <summary>Muda o que está guardado dos recursos (cria a linha se for a primeira vez).</summary>
    private async Task DefinirRecursosAsync(Jogador j, int? vida = null, int? stamina = null)
    {
        await StaminaAsync(j);
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

    [Fact]
    public async Task Esquiva_gasta_15_de_stamina_e_avisa_o_dono_e_quem_esta_na_area()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);

        Esquivar(ana, direcao: 90f);

        var recursos = await ana.Cliente.ReceberAsync<RecursosMudaram>(
            CombateMessageIds.RecursosMudaram
        );
        Assert.Equal(100 - RegrasEsquiva.CustoDeStamina, recursos.StaminaAtual);
        Assert.Equal(RegrasRecursos.StaminaBase, recursos.StaminaMaxima);

        foreach (var jogador in new[] { ana, beto })
        {
            var iniciada = await IniciadaAsync(jogador);
            Assert.Equal(ana.Id, iniciada.PersonagemId);
            Assert.Equal(90f, iniciada.DirecaoY);
            Assert.Equal(RegrasEsquiva.DuracaoMs, iniciada.DuracaoMs);
        }

        Assert.Equal(100 - RegrasEsquiva.CustoDeStamina, await StaminaAsync(ana));
        Assert.False(beto.Recebeu(CombateMessageIds.RecursosMudaram));
    }

    [Fact]
    public async Task Sem_stamina_a_esquiva_e_recusada_e_nada_e_gasto()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: RegrasEsquiva.CustoDeStamina - 1);

        Esquivar(ana, sequencia: 7);

        var recusada = await RecusadaAsync(ana);
        Assert.Equal(7, recusada.Sequencia);
        Assert.Equal(CodigosErroCombate.StaminaInsuficiente, recusada.Codigo);
        Assert.Equal(RegrasEsquiva.CustoDeStamina - 1, await StaminaAsync(ana));
        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaIniciada));
    }

    [Fact]
    public async Task Com_exatamente_15_de_stamina_a_esquiva_vale_e_zera_o_stamina()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: RegrasEsquiva.CustoDeStamina);

        Esquivar(ana);

        await IniciadaAsync(ana);
        Assert.Equal(0, await StaminaAsync(ana));
    }

    [Fact]
    public async Task Caido_nao_esquiva()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, vida: 0, stamina: 100);

        Esquivar(ana, sequencia: 3);

        var recusada = await RecusadaAsync(ana);
        Assert.Equal(3, recusada.Sequencia);
        Assert.Equal(CodigosErroCombate.VoceEstaCaido, recusada.Codigo);
        Assert.Equal(100, await StaminaAsync(ana));
    }

    [Fact]
    public async Task Esquiva_durante_atordoamento_e_recusada_com_VoceEstaAtordoado()
    {
        using var ana = await NovoJogadorAsync();
        var efeitos = servidor.Falso.Services.GetRequiredService<GerenciadorDeEfeitosDeStatus>();
        efeitos.Aplicar(ana.Id, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 5.0);

        Esquivar(ana, sequencia: 4);

        var recusada = await RecusadaAsync(ana);
        Assert.Equal(4, recusada.Sequencia);
        Assert.Equal(CodigosErroCombate.VoceEstaAtordoado, recusada.Codigo);
        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaIniciada));
    }

    [Fact]
    public async Task Recusada_por_falta_de_stamina_nao_prende_o_personagem()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 0);
        Esquivar(ana, sequencia: 1);
        Assert.Equal(CodigosErroCombate.StaminaInsuficiente, (await RecusadaAsync(ana)).Codigo);

        // Logo em seguida, com stamina, vale: a recusa liberou a reserva da ação.
        await DefinirRecursosAsync(ana, stamina: 50);
        Esquivar(ana, sequencia: 2);

        await IniciadaAsync(ana);
        Assert.Equal(50 - RegrasEsquiva.CustoDeStamina, await StaminaAsync(ana));
    }

    [Fact]
    public async Task Segunda_esquiva_durante_a_primeira_e_recusada_e_o_stamina_sai_uma_vez_so()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);

        Esquivar(ana, sequencia: 1);
        Esquivar(ana, sequencia: 2);

        var recusada = await RecusadaAsync(ana);
        Assert.Equal(2, recusada.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusada.Codigo);
        await TempoRealFixture.EsperarAsync(
            () => ana.Recebeu(CombateMessageIds.EsquivaIniciada),
            "EsquivaIniciada"
        );
        Assert.Equal(100 - RegrasEsquiva.CustoDeStamina, await StaminaAsync(ana));
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.EsquivaIniciada));
    }

    [Fact]
    public async Task Duas_esquivas_seguidas_gastam_stamina_duas_vezes()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);

        Esquivar(ana, sequencia: 1);
        await IniciadaAsync(ana);
        await Task.Delay(RegrasEsquiva.DuracaoMs + 100);
        Esquivar(ana, sequencia: 2);
        await IniciadaAsync(ana);

        Assert.Equal(100 - (2 * RegrasEsquiva.CustoDeStamina), await StaminaAsync(ana));
    }

    [Fact]
    public async Task Esquiva_durante_um_ataque_e_recusada()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);

        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Pesado, Sequencia = 1 }
        );
        await ana.Cliente.ReceberAsync<AtaqueIniciado>(CombateMessageIds.AtaqueIniciado);
        Esquivar(ana, sequencia: 2);

        var recusada = await RecusadaAsync(ana);
        Assert.Equal(2, recusada.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusada.Codigo);
        Assert.Equal(
            100 - RegrasAtaque.CustoDeStamina(TipoDeAtaque.Pesado),
            await StaminaAsync(ana)
        );
    }

    [Fact]
    public async Task Ataque_durante_a_esquiva_e_recusado()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);

        Esquivar(ana, sequencia: 1);
        await IniciadaAsync(ana);
        ana.Cliente.Enviar(
            CombateMessageIds.IntencaoDeAtaque,
            new IntencaoDeAtaque { Tipo = TipoDeAtaque.Pesado, Sequencia = 2 }
        );

        var recusado = await ana.Cliente.ReceberAsync<AtaqueRecusado>(
            CombateMessageIds.AtaqueRecusado
        );
        Assert.Equal(2, recusado.Sequencia);
        Assert.Equal(CodigosErroCombate.AcaoEmAndamento, recusado.Codigo);
        Assert.Equal(100 - RegrasEsquiva.CustoDeStamina, await StaminaAsync(ana));
    }

    [Fact]
    public async Task Depois_da_esquiva_o_mundo_aceita_o_deslocamento_mais_rapido_que_a_corrida()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirRecursosAsync(ana, stamina: 100);
        var z = ValidadorDeMovimento.Nascimento.Z;

        // Um passo curto zera a contagem de tempo do Mundo logo antes da esquiva.
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento { Z = z + 0.01f, Sequencia = 1 }
        );
        // O servidor não devolve a posição ao dono (ADR 0024): confere a dele.
        await PosicaoNoServidor.EsperarAsync(
            servidor.Falso.Services,
            ana.Id,
            p => p.Z == z + 0.01f
        );

        Esquivar(ana);
        await IniciadaAsync(ana);
        await Task.Delay(300);

        // 2,8 m em pouco mais de 0,3 s: correndo não dá (5,5 m/s), na esquiva dá (8,3 m/s).
        ana.Cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento { Z = z + 2.8f, Sequencia = 2 }
        );
        var posicao = await PosicaoNoServidor.EsperarAsync(
            servidor.Falso.Services,
            ana.Id,
            p => p.Z == z + 2.8f
        );

        Assert.Equal(z + 2.8f, posicao.Z);
    }

    [Fact]
    public async Task Fora_do_mundo_a_esquiva_e_descartada_sem_gastar_stamina()
    {
        using var ana = await NovoJogadorAsync(naCidade: false);
        await DefinirRecursosAsync(ana, stamina: 100);

        Esquivar(ana);
        // Um pedido conhecido depois: quando a resposta dele chega, a esquiva já foi tratada.
        ana.Cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await ana.Cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaIniciada));
        Assert.False(ana.Recebeu(CombateMessageIds.EsquivaRecusada));
        Assert.Equal(100, await StaminaAsync(ana));
    }
}
