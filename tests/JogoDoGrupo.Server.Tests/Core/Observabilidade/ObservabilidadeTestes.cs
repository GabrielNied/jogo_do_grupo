using JogoDoGrupo.Server.Core.Observabilidade;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace JogoDoGrupo.Server.Tests.Core.Observabilidade;

public class ObservabilidadeTestes
{
    [Fact]
    public async Task Saude_responde_ok_sem_autenticacao()
    {
        using var servidor = new Infra.ServidorDeTeste();
        using var cliente = servidor.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        resposta.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("Senha")]
    [InlineData("Token")]
    [InlineData("TextoChat")]
    public void Evento_com_dado_sensivel_nao_e_registrado(string propriedade)
    {
        var evento = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("teste", []),
            [new LogEventProperty(propriedade, new ScalarValue("segredo"))]
        );

        Assert.True(FiltroDadosSensiveis.TemDadoSensivel(evento));
    }

    [Fact]
    public void Contador_de_erros_descarta_os_com_mais_de_um_minuto()
    {
        var metricas = new MetricasDoServidor();
        metricas.RegistrarErro(DateTimeOffset.UtcNow.AddMinutes(-2));
        metricas.RegistrarErro(DateTimeOffset.UtcNow);

        var atual = metricas.Obter();

        Assert.Equal(1, atual.ErrosNoUltimoMinuto);
    }

    [Fact]
    public async Task Jogadores_online_acompanha_as_sessoes_autenticadas()
    {
        using var fixture = new TempoRealFixture();
        var metricas = fixture.Fabrica.Services.GetRequiredService<MetricasDoServidor>();
        Assert.Equal(0, metricas.Obter().JogadoresOnline);

        var (cliente, _, _) = await fixture.EntrarAsync();
        await TempoRealFixture.EsperarAsync(
            () => metricas.Obter().JogadoresOnline == 1,
            "métrica de jogador online"
        );

        cliente.Dispose();
        await TempoRealFixture.EsperarAsync(
            () => metricas.Obter().JogadoresOnline == 0,
            "métrica de jogador desconectado"
        );
    }
}
