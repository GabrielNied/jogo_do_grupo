using System.Net;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>
/// Servidor com a conferência real do baú (posição e recarga ligadas, #492), posições que o
/// teste controla e sorteio fixo de um item.
/// </summary>
public sealed class ServidorDeBau : IDisposable
{
    public ServidorDeTeste Base { get; } = new();
    public PosicoesFalsas Posicoes { get; } = new();
    public SorteadorFalso Sorteador { get; } = new();
    public WebApplicationFactory<Program> Servidor { get; }

    public ServidorDeBau()
    {
        Servidor = Base.WithWebHostBuilder(b =>
        {
            b.UseSetting("Itens:Bau:ExigirPosicao", "true");
            b.UseSetting("Itens:Bau:RecargaSegundos", "60");
            b.ConfigureServices(s =>
            {
                s.AddSingleton<IPosicoesDoMundo>(Posicoes);
                s.AddSingleton<ISorteadorBau>(Sorteador);
            });
        });
    }

    public void Dispose()
    {
        Servidor.Dispose();
        Base.Dispose();
    }
}

public class BauNoServidorTestes(ServidorDeBau servidor) : IClassFixture<ServidorDeBau>
{
    private async Task<(HttpClient Http, long Id)> NovoJogadorAsync()
    {
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Servidor);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (http, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private static Task<HttpResponseMessage> Abrir(HttpClient http, long id) =>
        http.PostAsync(RotasItens.BauDe(id), null);

    private static async Task AssertErro(
        HttpResponseMessage r,
        HttpStatusCode status,
        string codigo
    )
    {
        Assert.Equal(status, r.StatusCode);
        Assert.Equal(codigo, (await ServidorDeTeste.LerJsonAsync<ErroApi>(r)).Codigo);
    }

    private static async Task<int> TotalAsync(HttpClient http, long id)
    {
        var r = await http.GetAsync(RotasItens.InventarioDe(id));
        return (await ServidorDeTeste.LerJsonAsync<InventarioResponse>(r)).Entradas.Length;
    }

    [Fact]
    public async Task Longe_do_bau_recusa_sem_gravar_nem_gastar_a_recarga()
    {
        var (http, id) = await NovoJogadorAsync();
        servidor.Posicoes.Colocar(id, ConfigBau.Area, ConfigBau.X + 30f, ConfigBau.Z);

        await AssertErro(
            await Abrir(http, id),
            HttpStatusCode.Conflict,
            CodigosErroItens.BauLongeDemais
        );
        Assert.Equal(0, await TotalAsync(http, id));

        // Chegando perto, abre na hora: a recusa por distância não consumiu a recarga.
        servidor.Posicoes.Colocar(id, ConfigBau.Area, ConfigBau.X + 1f, ConfigBau.Z);
        Assert.Equal(HttpStatusCode.OK, (await Abrir(http, id)).StatusCode);
    }

    [Fact]
    public async Task Sem_posicao_ou_em_outra_area_recusa()
    {
        var (http, id) = await NovoJogadorAsync();

        await AssertErro(
            await Abrir(http, id),
            HttpStatusCode.Conflict,
            CodigosErroItens.BauLongeDemais
        );

        servidor.Posicoes.Colocar(id, "instancia:x", ConfigBau.X, ConfigBau.Z);
        await AssertErro(
            await Abrir(http, id),
            HttpStatusCode.Conflict,
            CodigosErroItens.BauLongeDemais
        );
        Assert.Equal(0, await TotalAsync(http, id));
    }

    [Fact]
    public async Task Abrir_de_novo_na_recarga_recusa_sem_gravar()
    {
        var (http, id) = await NovoJogadorAsync();
        servidor.Posicoes.Colocar(id, ConfigBau.Area, ConfigBau.X, ConfigBau.Z);

        Assert.Equal(HttpStatusCode.OK, (await Abrir(http, id)).StatusCode);
        var antes = await TotalAsync(http, id);

        await AssertErro(
            await Abrir(http, id),
            HttpStatusCode.TooManyRequests,
            CodigosErroItens.BauEmRecarga
        );
        Assert.Equal(antes, await TotalAsync(http, id));
    }

    [Fact]
    public async Task Dois_pedidos_simultaneos_dao_um_so_sorteio()
    {
        var (http, id) = await NovoJogadorAsync();
        servidor.Posicoes.Colocar(id, ConfigBau.Area, ConfigBau.X, ConfigBau.Z);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Abrir(http, id)));

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(5, respostas.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
        Assert.Equal(servidor.Sorteador.Resultado.Length, await TotalAsync(http, id));
    }
}
