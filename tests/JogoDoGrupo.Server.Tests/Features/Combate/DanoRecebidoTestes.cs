using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
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
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// O golpe de inimigo tira vida: o evento de Mobs é publicado direto (o círculo é decisão de
/// Mobs) e se confere a vida guardada e o aviso ao dono.
/// </summary>
public class DanoRecebidoTestes(ServidorDeAtaque servidor) : IClassFixture<ServidorDeAtaque>
{
    private async Task<Jogador> NovoJogadorAsync()
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
        return new Jogador(http, cliente, personagem.Id);
    }

    private void Golpear(
        long personagemId,
        int dano,
        TipoDeAtaqueDeInimigo tipo = TipoDeAtaqueDeInimigo.Golpe
    ) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, personagemId, dano, "cidade", tipo));

    private static async Task<RecursosResponse> RecursosAsync(Jogador j)
    {
        var resposta = await j.Http.GetAsync(RotasCombate.RecursosDe(j.Id));
        resposta.EnsureSuccessStatusCode();
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    private static Task<RecursosMudaram> MudouAsync(Jogador j) =>
        j.Cliente.ReceberAsync<RecursosMudaram>(CombateMessageIds.RecursosMudaram);

    private async Task DefinirVidaAsync(Jogador j, int vida)
    {
        await RecursosAsync(j); // Cria a linha.
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id); // O banco precisa ter o que a memória já mudou.
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        r.Vida = vida;
        await db.SaveChangesAsync();
        memoria.Descartar(j.Id); // E a memória precisa reler o que o teste mudou.
    }

    [Fact]
    public async Task Golpe_tira_a_vida_e_avisa_o_dono()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 30);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual - 30, aviso.VidaAtual);
        Assert.Equal(antes.VidaMaxima, aviso.VidaMaxima);
        Assert.Equal(antes.VidaAtual - 30, (await RecursosAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Golpe_so_tira_vida_de_quem_foi_atingido()
    {
        using var ana = await NovoJogadorAsync();
        using var beto = await NovoJogadorAsync();
        var vidaBeto = (await RecursosAsync(beto)).VidaAtual;

        Golpear(ana.Id, 10);

        await MudouAsync(ana);
        Assert.Equal(vidaBeto, (await RecursosAsync(beto)).VidaAtual);
        Assert.False(beto.Recebeu(CombateMessageIds.RecursosMudaram));
    }

    [Fact]
    public async Task Vida_nao_passa_de_zero()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirVidaAsync(ana, 5);

        Golpear(ana.Id, 50);

        Assert.Equal(0, (await MudouAsync(ana)).VidaAtual);
        Assert.Equal(0, (await RecursosAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Vida_em_zero_continua_em_zero()
    {
        using var ana = await NovoJogadorAsync();
        await DefinirVidaAsync(ana, 0);

        Golpear(ana.Id, 20);

        Assert.Equal(0, (await MudouAsync(ana)).VidaAtual);
        Assert.Equal(0, (await RecursosAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Golpes_simultaneos_somam_sem_perder_atualizacao()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);
        const int golpes = 8;

        await Task.WhenAll(
            Enumerable.Range(0, golpes).Select(_ => Task.Run(() => Golpear(ana.Id, 3)))
        );

        await TempoRealFixture.EsperarAsync(
            () => ana.Recebidas(CombateMessageIds.RecursosMudaram) == golpes,
            "RecursosMudaram de cada golpe"
        );
        Assert.Equal(antes.VidaAtual - golpes * 3, (await RecursosAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Personagem_sem_recursos_criados_perde_vida_a_partir_da_cheia()
    {
        using var ana = await NovoJogadorAsync(); // Nunca consultou os recursos.

        Golpear(ana.Id, 25);

        var aviso = await MudouAsync(ana);
        Assert.Equal(aviso.VidaMaxima - 25, aviso.VidaAtual);
    }

    [Fact]
    public async Task Personagem_que_nao_existe_nao_quebra_e_o_proximo_golpe_vale()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);

        var exception = Record.Exception(() => Golpear(long.MaxValue - 7, 10));
        Assert.Null(exception);

        Golpear(ana.Id, 10);

        Assert.Equal(antes.VidaAtual - 10, (await MudouAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Golpe_sem_dano_nao_muda_nada()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 0);
        Golpear(ana.Id, 10);

        Assert.Equal(antes.VidaAtual - 10, (await MudouAsync(ana)).VidaAtual);
        Assert.Equal(1, ana.Recebidas(CombateMessageIds.RecursosMudaram));
    }

    // Serviço próprio, com o controle de ações e o relógio nas mãos do teste: o instante do
    // golpe é decidido por quem testa, sem depender de tempo real.
    private ServicoDeDanoRecebido ServicoComRelogio(
        ControleDeAcoes acoes,
        Func<long> relogioMs,
        ServicoDeQueda? queda = null
    ) =>
        new(
            servidor.Falso.Services.GetRequiredService<ServidorTempoReal>(),
            servidor.Falso.Services.GetRequiredService<IServiceScopeFactory>(),
            servidor.Falso.Services.GetRequiredService<ILogger<ServicoDeDanoRecebido>>(),
            queda: queda,
            acoes: acoes,
            relogioMs: relogioMs
        );

    [Fact]
    public async Task Golpe_nos_primeiros_0_3_s_da_esquiva_nao_tira_vida_e_depois_tira()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);
        var acoes = new ControleDeAcoes();
        long agora = 10_000;
        var servico = ServicoComRelogio(acoes, () => agora);
        acoes.TentarOcupar(ana.Id, 10_000, RegrasEsquiva.DuracaoMs, out _);
        var golpe = new GolpeDeInimigoAcertou(1, 1, ana.Id, 30, "cidade");

        agora = 10_299; // ainda na janela: o dano some
        servico.AoGolpeDeInimigo(golpe);
        await Task.Delay(300);
        Assert.False(ana.Recebeu(CombateMessageIds.RecursosMudaram));
        Assert.Equal(antes.VidaAtual, (await RecursosAsync(ana)).VidaAtual);

        agora = 10_300; // acabou a janela: o golpe, ainda no volume, tira vida
        servico.AoGolpeDeInimigo(golpe);
        Assert.Equal(antes.VidaAtual - 30, (await MudouAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Golpe_nos_primeiros_2_s_apos_levantar_nao_tira_vida_e_depois_tira()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);
        var acoes = new ControleDeAcoes();
        long agora = 10_000;
        var queda = servidor.Falso.Services.GetRequiredService<ServicoDeQueda>();
        var servico = ServicoComRelogio(acoes, () => agora, queda);

        // Ana cai aos 10_000 e é levantada por Beto aos 13_000
        queda.Controle.Cair(ana.Id, agora, out _);
        queda.Controle.Comecar(2, ana.Id, agora, () => true, out _);
        agora = 13_000;
        queda.Controle.Resolver(ana.Id, agora);
        queda.LimparGravandoParaTeste(ana.Id);

        var golpe = new GolpeDeInimigoAcertou(1, 1, ana.Id, 30, "cidade");

        // Em 1,9 s após levantar (14_900 ms), o golpe não causa dano devido à imunidade de 2 s
        agora = 14_900;
        servico.AoGolpeDeInimigo(golpe);
        await Task.Delay(300);
        Assert.False(ana.Recebeu(CombateMessageIds.RecursosMudaram));
        Assert.Equal(antes.VidaAtual, (await RecursosAsync(ana)).VidaAtual);

        // Em 2,0 s após levantar (15_000 ms), a imunidade expirou e o golpe dá dano
        agora = 15_000;
        servico.AoGolpeDeInimigo(golpe);
        Assert.Equal(antes.VidaAtual - 30, (await MudouAsync(ana)).VidaAtual);
    }

    [Fact]
    public void Publicar_nao_bloqueia_quem_publica()
    {
        // O banco roda numa tarefa: mesmo com a trava ocupada, o evento volta na hora.
        var inicio = DateTime.UtcNow;
        Golpear(long.MaxValue - 9, 10);
        Assert.True(DateTime.UtcNow - inicio < TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(40, 40)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    public void Dano_final_nao_e_negativo(int golpe, int esperado) =>
        Assert.Equal(esperado, ServicoDeDanoRecebido.DanoFinal(golpe));

    [Fact]
    public async Task Golpe_comum_de_inimigo_tira_vida_mas_nao_emite_acao_interrompida()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 20, TipoDeAtaqueDeInimigo.Golpe);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual - 20, aviso.VidaAtual);
        Assert.False(ana.Recebeu(CombateMessageIds.AcaoInterrompida));
    }

    [Fact]
    public async Task Golpe_especial_de_inimigo_tira_vida_e_emite_acao_interrompida()
    {
        using var ana = await NovoJogadorAsync();
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 25, TipoDeAtaqueDeInimigo.Investida);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual - 25, aviso.VidaAtual);
        var interrompida = await ana.Cliente.ReceberAsync<AcaoInterrompida>(
            CombateMessageIds.AcaoInterrompida
        );
        Assert.True(interrompida.Jogador);
        Assert.Equal(ana.Id, interrompida.AlvoId);
    }

    private static void Guardar(Jogador j, bool segurando) =>
        j.Cliente.Enviar(
            CombateMessageIds.IntencaoDeGuarda,
            new IntencaoDeGuarda { Segurando = segurando }
        );

    private async Task DefinirStaminaAsync(Jogador j, int stamina)
    {
        await RecursosAsync(j);
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(j.Id);
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == j.Id);
        r.Stamina = stamina;
        await db.SaveChangesAsync();
        memoria.Descartar(j.Id);
    }

    [Fact]
    public async Task Defesa_maior_ou_igual_a_um_e_meio_da_potencia_anula_o_dano_direto()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { DefesaFisica = 45 };
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 30);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual, aviso.VidaAtual);
        Assert.Equal(antes.VidaAtual, (await RecursosAsync(ana)).VidaAtual);
        Assert.False(ana.Recebeu(CombateMessageIds.DefesaInsuficiente));
    }

    [Fact]
    public async Task Defesa_parcial_reduz_o_dano_proporcionalmente()
    {
        using var ana = await NovoJogadorAsync();
        // 22.5 é 50% de 45 (1.5 * 30). Redução de 50% de 80% = 40%. Dano 30 * 0.60 = 18.
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { DefesaFisica = 22.5 };
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 30);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual - 18, aviso.VidaAtual);
        Assert.Equal(antes.VidaAtual - 18, (await RecursosAsync(ana)).VidaAtual);
    }

    [Fact]
    public async Task Guarda_quebrada_com_defesa_insuficiente_emite_aviso_ao_cliente()
    {
        using var ana = await NovoJogadorAsync();
        ana.Cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await ana.Cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        await DefinirStaminaAsync(ana, 10);
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { DefesaFisica = 15 };

        Guardar(ana, true);
        await ana.Cliente.ReceberAsync<GuardaMudou>(CombateMessageIds.GuardaMudou);

        Golpear(ana.Id, 30);

        var quebrou = await ana.Cliente.ReceberAsync<GuardaMudou>(CombateMessageIds.GuardaMudou);
        Assert.True(quebrou.Quebrou);

        var avisoDefesa = await ana.Cliente.ReceberAsync<DefesaInsuficiente>(
            CombateMessageIds.DefesaInsuficiente
        );
        Assert.Equal(ana.Id, avisoDefesa.PersonagemId);
        Assert.True(avisoDefesa.DanoResidual > 0);
        Assert.Equal(30, avisoDefesa.DanoOriginal);
    }

    [Fact]
    public async Task Golpe_magico_raios_mitiga_pela_defesa_magica()
    {
        using var ana = await NovoJogadorAsync();
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos
        {
            DefesaMagica = 45,
            DefesaFisica = 0,
        };
        var antes = await RecursosAsync(ana);

        Golpear(ana.Id, 30, TipoDeAtaqueDeInimigo.Raios);

        var aviso = await MudouAsync(ana);
        Assert.Equal(antes.VidaAtual, aviso.VidaAtual);
    }
}
