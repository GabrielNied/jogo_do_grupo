using System.Net;
using JogoDoGrupo.Robos;
using JogoDoGrupo.Server.Tests.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Observabilidade;

/// <summary><c>GET /admin/metricas</c> pelo caminho que os robôs de carga usam (#150).</summary>
public class MetricasAdminTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public async Task Token_assinado_com_a_chave_do_servidor_le_o_tick()
    {
        using var http = servidor.CreateClient();
        var chave = servidor.Services.GetRequiredService<IConfiguration>()["Jwt:Chave"];

        var opcoes = new OpcoesDeCarga { ChaveJwt = chave };

        // O laço acabou de subir: espera ter passos para medir o ritmo.
        var limite = DateTime.UtcNow + ClienteTempoRealDeTeste.Espera;
        TickDoServidor? tick;
        do
        {
            await Task.Delay(50);
            tick = await TokenDeMetricas.LerAsync(http, opcoes, CancellationToken.None);
        } while (tick!.PassosPorSegundo == 0 && DateTime.UtcNow < limite);

        Assert.True(tick.PassosPorSegundo > 0);
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        using var http = servidor.CreateClient();

        var resposta = await http.GetAsync(TokenDeMetricas.Rota);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_de_jogador_comum_responde_403()
    {
        using var http = await servidor.CriarClienteLogadoAsync();

        var resposta = await http.GetAsync(TokenDeMetricas.Rota);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Chave_errada_nao_le_o_tick()
    {
        using var http = servidor.CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TokenDeMetricas.LerAsync(
                http,
                new OpcoesDeCarga { ChaveJwt = new string('x', 64) },
                CancellationToken.None
            )
        );
    }
}
