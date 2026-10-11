using System.Net;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Features.Personagem;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

public class ExperienciaDoPersonagemTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public async Task Ganho_que_cruza_dois_niveis_sobe_os_dois_e_da_dez_pontos()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var ganho =
            RegrasExperiencia.XpParaOProximoNivel(1) + RegrasExperiencia.XpParaOProximoNivel(2);

        await ConcederAsync(id, (int)ganho, sequencia: 1);

        var progresso = await ProgressoAsync(cliente, id);
        Assert.Equal(3, progresso.Nivel);
        Assert.Equal(0, progresso.XpAtual);
        Assert.Equal(10, progresso.PontosLivres);
    }

    [Fact]
    public async Task A_mesma_morte_nao_premia_duas_vezes_e_outra_sequencia_premia()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);

        await ConcederAsync(id, 40, sequencia: 7, inimigoId: 3);
        await ConcederAsync(id, 40, sequencia: 7, inimigoId: 3);
        Assert.Equal(40, (await ProgressoAsync(cliente, id)).XpAtual);

        await ConcederAsync(id, 40, sequencia: 8, inimigoId: 3);
        Assert.Equal(80, (await ProgressoAsync(cliente, id)).XpAtual);
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
        await ConcederAsync(id, (int)RegrasExperiencia.XpParaOProximoNivel(1), sequencia: 1);

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
        await ConcederAsync(id, (int)RegrasExperiencia.XpParaOProximoNivel(1), sequencia: 1);
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
        await ConcederAsync(id, (int)RegrasExperiencia.XpParaOProximoNivel(1), sequencia: 1);

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
        await ConcederAsync(id, (int)RegrasExperiencia.XpParaOProximoNivel(1), sequencia: 1);
        var gasto = await GastarAsync(cliente, id, new PontosDeAtributos { Vitalidade = 5 });
        Assert.Equal(HttpStatusCode.OK, gasto.StatusCode);

        var recursos = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        Assert.Equal(HttpStatusCode.OK, recursos.StatusCode);
        var corpo = await ServidorDeTeste.LerJsonAsync<RecursosResponse>(recursos);
        Assert.Equal(RegrasRecursos.VidaMaxima(5), corpo.VidaMaxima);
    }

    private static long _proximoInimigo = 1000;

    private ExperienciaDoPersonagem Experiencia =>
        servidor.Services.GetRequiredService<ExperienciaDoPersonagem>();

    private Task ConcederAsync(
        long personagemId,
        int experiencia,
        long sequencia,
        long? inimigoId = null
    )
    {
        var derrota = new InimigoDerrotado(
            inimigoId ?? Interlocked.Increment(ref _proximoInimigo),
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
        return Experiencia.ConcederAsync(derrota, CancellationToken.None);
    }

    private static async Task<long> CriarPersonagemAsync(HttpClient cliente)
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("Xp"),
                Raca = Raca.Humano,
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
