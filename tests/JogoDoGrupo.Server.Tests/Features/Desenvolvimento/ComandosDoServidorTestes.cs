using System.Net;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Desenvolvimento;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Desenvolvimento;

public class ComandosDoServidorTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public async Task Teleporte_fora_do_mapa_e_recusado()
    {
        var comandos = servidor.Services.GetRequiredService<ComandosDoServidor>();
        var fora = await comandos.ExecutarAsync(1, "tp 99999 0 0", CancellationToken.None);
        Assert.False(fora.Aceito);
        Assert.Equal("Posição fora do mundo.", fora.Texto);
    }

    [Fact]
    public async Task Xp_soma_e_tira_e_os_outros_comandos_recusam_fora_do_mundo()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var id = await CriarPersonagemAsync(cliente);
        var comandos = servidor.Services.GetRequiredService<ComandosDoServidor>();

        var ganho = await comandos.ExecutarAsync(id, "xp 40", CancellationToken.None);
        Assert.True(ganho.Aceito);
        Assert.Equal(40, (await ProgressoAsync(cliente, id)).XpAtual);

        var perda = await comandos.ExecutarAsync(id, "xp -15", CancellationToken.None);
        Assert.True(perda.Aceito);
        Assert.Equal(25, (await ProgressoAsync(cliente, id)).XpAtual);
        Assert.Equal(1, (await ProgressoAsync(cliente, id)).Nivel);

        var teleporte = await comandos.ExecutarAsync(id, "tp 1 0 -20", CancellationToken.None);
        Assert.False(teleporte.Aceito);
        Assert.Equal("Você não está no mundo.", teleporte.Texto);

        var spawn = await comandos.ExecutarAsync(id, "spawn", CancellationToken.None);
        Assert.False(spawn.Aceito);
        Assert.Equal("Você não está no mundo.", spawn.Texto);

        var ajuda = await comandos.ExecutarAsync(id, "ajuda", CancellationToken.None);
        Assert.True(ajuda.Aceito);
    }

    [Fact]
    public async Task Personagem_inexistente_nao_ganha_experiencia()
    {
        var comandos = servidor.Services.GetRequiredService<ComandosDoServidor>();
        var resultado = await comandos.ExecutarAsync(
            long.MaxValue,
            "xp 10",
            CancellationToken.None
        );
        Assert.False(resultado.Aceito);
        Assert.Equal("Personagem não encontrado.", resultado.Texto);
    }

    private static async Task<long> CriarPersonagemAsync(HttpClient cliente)
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("Cmd"),
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
}

/// <summary>O executor delega; estes dublês só devolvem o que o teste quer ver.</summary>
public class ComandosDoServidorComDublesTestes
{
    [Fact]
    public async Task Comando_invalido_nao_chama_as_outras_funcionalidades()
    {
        var experiencia = new ExperienciaFalsa();
        var comandos = new ComandosDoServidor(experiencia, new TeleporteFalso(), new SpawnFalso());

        var resultado = await comandos.ExecutarAsync(1, "voar", CancellationToken.None);

        Assert.False(resultado.Aceito);
        Assert.Equal(0, experiencia.Chamadas);
    }

    private sealed class ExperienciaFalsa : IAjusteDeExperienciaDeTeste
    {
        public int Chamadas { get; private set; }

        public Task<(bool Aceito, string Texto)> AjustarAsync(
            long personagemId,
            long quantidade,
            CancellationToken cancelamento
        )
        {
            Chamadas++;
            return Task.FromResult((true, "ok"));
        }
    }

    private sealed class TeleporteFalso : ITeleporteDeTeste
    {
        public (bool Aceito, string Texto) Teleportar(
            long personagemId,
            float x,
            float y,
            float z
        ) => (true, "ok");
    }

    private sealed class SpawnFalso : ISpawnDeTeste
    {
        public (bool Aceito, string Texto) SpawnarPerto(long personagemId) => (true, "ok");
    }
}
