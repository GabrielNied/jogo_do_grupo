using System.Net;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

public sealed class ServidorDeEconomiaProximidade : IDisposable
{
    public ServidorDeTeste Base { get; } = new();
    public PosicoesFalsas Posicoes { get; } = new();
    public WebApplicationFactory<Program> Servidor { get; }

    public ServidorDeEconomiaProximidade()
    {
        Servidor = Base.WithWebHostBuilder(b =>
        {
            b.UseSetting("Economia:Npc:ExigirProximidade", "true");
            b.ConfigureServices(s => s.AddSingleton<IPosicoesDoMundo>(Posicoes));
        });
    }

    public void Dispose()
    {
        Servidor.Dispose();
        Base.Dispose();
    }
}

public class NpcEconomiaProximidadeTestes(ServidorDeEconomiaProximidade servidor)
    : IClassFixture<ServidorDeEconomiaProximidade>
{
    private async Task<(HttpClient Cliente, long Id)> NovoPersonagemAsync()
    {
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Servidor);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        return (cliente, id);
    }

    [Fact]
    public async Task Catalogo_LongeDoVendedor_Retorna409()
    {
        var (cliente, id) = await NovoPersonagemAsync();
        servidor.Posicoes.Colocar(
            id,
            ConfigNpcEconomia.Area,
            ConfigNpcEconomia.VendedorX + 30f,
            ConfigNpcEconomia.VendedorZ
        );

        var res = await cliente.GetAsync(RotasEconomia.LojaDe(id));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(res);
        Assert.Equal(CodigosErroEconomia.LongeDoNpc, erro.Codigo);
    }

    [Fact]
    public async Task Catalogo_PertoDoVendedor_RetornaOk()
    {
        var (cliente, id) = await NovoPersonagemAsync();
        servidor.Posicoes.Colocar(
            id,
            ConfigNpcEconomia.Area,
            ConfigNpcEconomia.VendedorX,
            ConfigNpcEconomia.VendedorZ
        );

        var res = await cliente.GetAsync(RotasEconomia.LojaDe(id));

        res.EnsureSuccessStatusCode();
    }
}
