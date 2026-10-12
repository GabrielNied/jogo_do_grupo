using JogoDoGrupo.Server.Core.Observabilidade;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
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
    [InlineData("sEnHa")]
    [InlineData("Authorization")]
    [InlineData("AccessToken")]
    public void Evento_com_dado_sensivel_e_registrado_mascarado(string propriedade)
    {
        var evento = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("teste", []),
            [new LogEventProperty(propriedade, new ScalarValue("segredo"))]
        );

        var destino = new DestinoDeTeste();
        var metricas = new MetricasDoServidor();
        using var logger = new LoggerConfiguration()
            .Enrich.With(new FiltroDadosSensiveis())
            .WriteTo.Sink(destino)
            .WriteTo.Sink(new ContadorDeErrosSink(metricas))
            .CreateLogger();
        logger.Write(evento);

        Assert.Same(evento, destino.Evento);
        Assert.Equal("***", Assert.IsType<ScalarValue>(evento.Properties[propriedade]).Value);
    }

    [Fact]
    public void Erro_mascarado_continua_contado_e_preserva_contexto()
    {
        var destino = new DestinoDeTeste();
        var metricas = new MetricasDoServidor();
        using var logger = new LoggerConfiguration()
            .Enrich.With(new FiltroDadosSensiveis())
            .WriteTo.Sink(destino)
            .WriteTo.Sink(new ContadorDeErrosSink(metricas))
            .CreateLogger();

        logger.Error("Falha {Contexto}: {Mensagem}, {Senha}", "login", "recusado", "segredo");

        Assert.Equal(1, metricas.Obter().ErrosNoUltimoMinuto);
        Assert.Equal(
            "login",
            Assert.IsType<ScalarValue>(destino.Evento!.Properties["Contexto"]).Value
        );
        Assert.Equal(
            "***",
            Assert.IsType<ScalarValue>(destino.Evento.Properties["Mensagem"]).Value
        );
        Assert.DoesNotContain("segredo", destino.Evento.RenderMessage());
    }

    [Fact]
    public void Mascara_dados_em_objetos_listas_e_dicionarios()
    {
        var destino = new DestinoDeTeste();
        using var logger = new LoggerConfiguration()
            .Enrich.With(new FiltroDadosSensiveis())
            .WriteTo.Sink(destino)
            .CreateLogger();

        logger.Information(
            "Dados {@Dados}",
            new
            {
                Contexto = "login",
                Contas = new[] { new { Senha = "segredo", Codigo = 42 } },
                Campos = new Dictionary<string, string> { ["Token"] = "segredo" },
            }
        );

        var texto = destino.Evento!.RenderMessage();
        Assert.DoesNotContain("segredo", texto);
        Assert.Contains("login", texto);
        Assert.Contains("42", texto);
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

    private sealed class DestinoDeTeste : ILogEventSink
    {
        public LogEvent? Evento { get; private set; }

        public void Emit(LogEvent evento) => Evento = evento;
    }
}
