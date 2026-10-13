using System.Net;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Personagem;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

public class ExperienciaDoPersonagemTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public async Task Ganho_que_cruza_dois_niveis_sobe_os_dois_e_da_os_pontos_da_arvore()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var ganho =
            RegrasExperiencia.XpParaOProximoNivel(1) + RegrasExperiencia.XpParaOProximoNivel(2);

        await ConcederAsync(id, (int)ganho, sequencia: 1);

        var progresso = await ProgressoAsync(cliente, id);
        Assert.Equal(3, progresso.Nivel);
        Assert.Equal(0, progresso.XpAtual);
        Assert.Equal(2 * ConfigExperiencia.PontosPorNivel, progresso.PontosLivres);
    }

    [Fact]
    public async Task A_mesma_morte_nao_premia_duas_vezes_e_outra_sequencia_premia()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);

        await ConcederAsync(id, 20, sequencia: 7, inimigoId: 3);
        await ConcederAsync(id, 20, sequencia: 7, inimigoId: 3);
        Assert.Equal(20, (await ProgressoAsync(cliente, id)).XpAtual);

        await ConcederAsync(id, 20, sequencia: 8, inimigoId: 3);
        Assert.Equal(40, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    [Fact]
    public async Task Quem_nao_participou_nao_ganha_experiencia()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var participante = await CriarPersonagemAsync(cliente);
        var deFora = await CriarPersonagemAsync(cliente);

        await ConcederAsync(participante, 40, sequencia: 1);

        Assert.Equal(40, (await ProgressoAsync(cliente, participante)).XpAtual);
        Assert.Equal(0, (await ProgressoAsync(cliente, deFora)).XpAtual);
    }

    [Fact]
    public async Task Experiencia_diminui_por_nivel_acima_e_zera_no_quinto()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var abaixo = await CriarPersonagemAsync(cliente);
        var umAcima = await CriarPersonagemAsync(cliente);
        var quatroAcima = await CriarPersonagemAsync(cliente);
        var cincoAcima = await CriarPersonagemAsync(cliente);
        await DefinirNivelAsync(abaixo, 2);
        await DefinirNivelAsync(umAcima, 4);
        await DefinirNivelAsync(quatroAcima, 7);
        await DefinirNivelAsync(cincoAcima, 8);

        await Experiencia.ConcederAsync(
            Derrota([abaixo, umAcima, quatroAcima, cincoAcima], 101, 1, 10, nivelInimigo: 3),
            CancellationToken.None
        );

        Assert.Equal(101, (await ProgressoAsync(cliente, abaixo)).XpAtual);
        Assert.Equal(80, (await ProgressoAsync(cliente, umAcima)).XpAtual);
        Assert.Equal(20, (await ProgressoAsync(cliente, quatroAcima)).XpAtual);
        Assert.Equal(0, (await ProgressoAsync(cliente, cincoAcima)).XpAtual);
    }

    [Fact]
    public async Task Nivel_de_inimigo_desconhecido_concede_experiencia_base()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        await DefinirNivelAsync(id, 20);

        await ConcederAsync(id, 100, sequencia: 1, nivelInimigo: 0);

        Assert.Equal(100, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    [Fact]
    public async Task No_nivel_maximo_o_excedente_fica_guardado_e_nao_gera_ponto()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);

        await ConcederAsync(id, int.MaxValue, sequencia: 1);
        var noTeto = await ProgressoAsync(cliente, id);
        Assert.Equal(RegrasExperiencia.NivelMaximo, noTeto.Nivel);

        await ConcederAsync(id, 500, sequencia: 2, nivelInimigo: 100);
        var depois = await ProgressoAsync(cliente, id);
        Assert.Equal(RegrasExperiencia.NivelMaximo, depois.Nivel);
        Assert.Equal(noTeto.XpAtual + 500, depois.XpAtual);
        Assert.Equal(noTeto.PontosLivres, depois.PontosLivres);
    }

    [Fact]
    public async Task Depois_de_reiniciar_a_mesma_sequencia_de_mortes_volta_a_dar_experiencia()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        await ConcederAsync(id, 20, sequencia: 1, inimigoId: 4);
        Assert.Equal(20, (await ProgressoAsync(cliente, id)).XpAtual);

        // Reinício: serviço novo (trava vazia), mesmo banco, mesmo par inimigo + sequência.
        var reiniciado = NovoServico(servidor.Services.GetRequiredService<IServiceScopeFactory>());
        await reiniciado.ConcederAsync(Derrota(id, 20, 1, 4), CancellationToken.None);

        Assert.Equal(40, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    [Fact]
    public async Task Banco_ocupado_na_primeira_tentativa_concede_na_segunda_uma_vez_so()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var escopos = new EscoposQueFalham(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            falhas: 1
        );
        var servico = NovoServico(escopos);
        var derrota = Derrota(id, 20, 1, 5);

        await servico.ConcederAsync(derrota, CancellationToken.None);
        await servico.ConcederAsync(derrota, CancellationToken.None);

        Assert.Equal(20, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    [Fact]
    public async Task Sem_conseguir_gravar_a_morte_nao_fica_travada()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var escopos = new EscoposQueFalham(
            servidor.Services.GetRequiredService<IServiceScopeFactory>(),
            falhas: ExperienciaDoPersonagem.TentativasDeGravacao
        );
        var servico = NovoServico(escopos);
        var derrota = Derrota(id, 20, 1, 6);

        await Assert.ThrowsAsync<SqliteException>(() =>
            servico.ConcederAsync(derrota, CancellationToken.None)
        );
        Assert.Equal(0, (await ProgressoAsync(cliente, id)).XpAtual);

        await servico.ConcederAsync(derrota, CancellationToken.None);
        Assert.Equal(20, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    [Fact]
    public async Task Duas_mortes_ao_mesmo_tempo_somam_as_duas()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);

        await Task.WhenAll(
            ConcederAsync(id, 15, sequencia: 1),
            ConcederAsync(id, 15, sequencia: 2),
            ConcederAsync(id, 15, sequencia: 3)
        );

        Assert.Equal(45, (await ProgressoAsync(cliente, id)).XpAtual);
    }

    private static long _proximoInimigo = 1000;

    private ExperienciaDoPersonagem Experiencia =>
        servidor.Services.GetRequiredService<ExperienciaDoPersonagem>();

    private ExperienciaDoPersonagem NovoServico(IServiceScopeFactory escopos) =>
        new(
            escopos,
            servidor.Services.GetRequiredService<ServidorTempoReal>(),
            servidor.Services.GetRequiredService<IPosicoesDoMundo>(),
            NullLogger<ExperienciaDoPersonagem>.Instance
        );

    /// <summary>Simula banco ocupado nas primeiras criações de escopo.</summary>
    private sealed class EscoposQueFalham(IServiceScopeFactory real, int falhas)
        : IServiceScopeFactory
    {
        private int _restantes = falhas;

        public IServiceScope CreateScope()
        {
            if (Interlocked.Decrement(ref _restantes) >= 0)
            {
                throw new SqliteException("database is locked", 5);
            }

            return real.CreateScope();
        }
    }

    private static InimigoDerrotado Derrota(
        long personagemId,
        int experiencia,
        long sequencia,
        long inimigoId,
        int nivelInimigo = 1
    ) =>
        new(
            inimigoId,
            1,
            VarianteDeCriatura.Normal,
            experiencia,
            1,
            "cidade",
            0,
            0,
            [personagemId],
            sequencia,
            nivelInimigo
        );

    private static InimigoDerrotado Derrota(
        IReadOnlyList<long> participantes,
        int experiencia,
        long sequencia,
        long inimigoId,
        int nivelInimigo
    ) =>
        new(
            inimigoId,
            1,
            VarianteDeCriatura.Normal,
            experiencia,
            1,
            "cidade",
            0,
            0,
            participantes,
            sequencia,
            nivelInimigo
        );

    private Task ConcederAsync(
        long personagemId,
        int experiencia,
        long sequencia,
        long? inimigoId = null,
        int nivelInimigo = 1
    ) =>
        Experiencia.ConcederAsync(
            Derrota(
                personagemId,
                experiencia,
                sequencia,
                inimigoId ?? Interlocked.Increment(ref _proximoInimigo),
                nivelInimigo
            ),
            CancellationToken.None
        );

    private static async Task<long> CriarPersonagemAsync(HttpClient cliente)
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("Xp"),
                Raca = Raca.Valdren,
            }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    private static async Task<ProgressoResponse> ProgressoAsync(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasPersonagem.ProgressoDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<ProgressoResponse>(resposta);
    }

    private async Task DefinirPontosLivresAsync(long id, int pontos)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var personagem = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .SingleAsync(p => p.Id == id);
        personagem.PontosLivres = pontos;
        await db.SaveChangesAsync();
    }

    private async Task DefinirNivelAsync(long id, int nivel)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var personagem = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .SingleAsync(p => p.Id == id);
        personagem.Nivel = nivel;
        await db.SaveChangesAsync();
    }
}
