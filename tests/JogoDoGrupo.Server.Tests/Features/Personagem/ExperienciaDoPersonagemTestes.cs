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
    public async Task Ganho_que_cruza_dois_niveis_sobe_os_dois_sem_ponto()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var ganho =
            RegrasExperiencia.XpParaOProximoNivel(1) + RegrasExperiencia.XpParaOProximoNivel(2);

        await ConcederAsync(id, (int)ganho, sequencia: 1);

        var progresso = await ProgressoAsync(cliente, id);
        Assert.Equal(3, progresso.Nivel);
        Assert.Equal(0, progresso.XpAtual);
        Assert.Equal(0, progresso.PontosLivres);
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
    public async Task No_nivel_maximo_o_excedente_fica_guardado_e_nao_gera_ponto()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);

        await ConcederAsync(id, int.MaxValue, sequencia: 1);
        var noTeto = await ProgressoAsync(cliente, id);
        Assert.Equal(RegrasExperiencia.NivelMaximo, noTeto.Nivel);

        await ConcederAsync(id, 500, sequencia: 2);
        var depois = await ProgressoAsync(cliente, id);
        Assert.Equal(RegrasExperiencia.NivelMaximo, depois.Nivel);
        Assert.Equal(noTeto.XpAtual + 500, depois.XpAtual);
        Assert.Equal(noTeto.PontosLivres, depois.PontosLivres);
    }

    [Fact]
    public async Task Gasto_exato_aplica_e_um_ponto_a_mais_nao_muda_nada()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        await DefinirPontosLivresAsync(id, 5);

        var gasto = await GastarAsync(cliente, id, new PontosDeAtributos { Forca = 5 });
        Assert.Equal(HttpStatusCode.OK, gasto.StatusCode);
        var progresso = await ServidorDeTeste.LerJsonAsync<ProgressoResponse>(gasto);
        Assert.Equal(0, progresso.PontosLivres);
        Assert.Equal(5, progresso.Distribuidos.Forca);

        var recusa = await GastarAsync(cliente, id, new PontosDeAtributos { Forca = 1 });
        Assert.Equal(HttpStatusCode.Conflict, recusa.StatusCode);
        Assert.Equal(
            CodigosErroPersonagem.PontosInsuficientes,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(recusa)).Codigo
        );
        Assert.Equal(5, (await ProgressoAsync(cliente, id)).Distribuidos.Forca);
    }

    [Fact]
    public async Task Dois_gastos_simultaneos_nao_usam_os_mesmos_pontos()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        await DefinirPontosLivresAsync(id, 5);
        var pedido = new PontosDeAtributos { Vitalidade = 5 };

        var respostas = await Task.WhenAll(
            GastarAsync(cliente, id, pedido),
            GastarAsync(cliente, id, pedido)
        );

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var progresso = await ProgressoAsync(cliente, id);
        Assert.Equal(0, progresso.PontosLivres);
        Assert.Equal(5, progresso.Distribuidos.Vitalidade);
    }

    [Fact]
    public async Task Pedido_invalido_e_personagem_de_outra_conta_sao_recusados()
    {
        var dono = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(dono);
        await DefinirPontosLivresAsync(id, 5);

        var negativo = await GastarAsync(dono, id, new PontosDeAtributos { Forca = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, negativo.StatusCode);
        var zero = await GastarAsync(dono, id, new PontosDeAtributos());
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(5, (await ProgressoAsync(dono, id)).PontosLivres);

        var outro = await servidor.CriarClienteLogadoAsync();
        var alheio = await GastarAsync(outro, id, new PontosDeAtributos { Forca = 1 });
        Assert.Equal(HttpStatusCode.NotFound, alheio.StatusCode);
        var progressoAlheio = await outro.GetAsync(RotasPersonagem.ProgressoDe(id));
        Assert.Equal(HttpStatusCode.NotFound, progressoAlheio.StatusCode);
    }

    [Fact]
    public async Task Vitalidade_distribuida_aumenta_a_vida_maxima()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        await DefinirPontosLivresAsync(id, 5);
        var gasto = await GastarAsync(cliente, id, new PontosDeAtributos { Vitalidade = 5 });
        Assert.Equal(HttpStatusCode.OK, gasto.StatusCode);

        var recursos = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        Assert.Equal(HttpStatusCode.OK, recursos.StatusCode);
        var corpo = await ServidorDeTeste.LerJsonAsync<RecursosResponse>(recursos);
        Assert.Equal(RegrasRecursos.VidaMaxima(5), corpo.VidaMaxima);
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
        long inimigoId
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
            sequencia
        );

    private Task ConcederAsync(
        long personagemId,
        int experiencia,
        long sequencia,
        long? inimigoId = null
    ) =>
        Experiencia.ConcederAsync(
            Derrota(
                personagemId,
                experiencia,
                sequencia,
                inimigoId ?? Interlocked.Increment(ref _proximoInimigo)
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

    private static Task<HttpResponseMessage> GastarAsync(
        HttpClient cliente,
        long id,
        PontosDeAtributos pontos
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.AtributosDe(id),
            new DistribuirAtributosRequest { Pontos = pontos }
        );
}
