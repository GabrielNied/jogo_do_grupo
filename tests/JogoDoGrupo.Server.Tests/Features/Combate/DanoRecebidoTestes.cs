using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// O golpe de inimigo tira vida: o evento de Mobs é publicado direto (o círculo é decisão de
/// Mobs) e se confere a vida guardada e o aviso ao dono.
/// </summary>
public class DanoRecebidoTestes(ServidorDeAtaque servidor) : IClassFixture<ServidorDeAtaque>
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
        return new Jogador(http, cliente, personagem.Id);
    }

    private void Golpear(long personagemId, int dano) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, personagemId, dano, "cidade"));

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
}
