using JogoDoGrupo.Server.Features.Mundo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class PoliticaDeVelocidadeDeTesteTestes
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Chave_desligada_fica_desligada_sem_aviso(string ambiente)
    {
        var log = new LogColetor();

        Assert.False(Ler("false", ambiente, log));
        Assert.Empty(log.Avisos);
    }

    [Fact]
    public void Chave_ausente_fica_desligada()
    {
        var log = new LogColetor();

        Assert.False(Ler(null, "Development", log));
        Assert.Empty(log.Avisos);
    }

    [Fact]
    public void Development_com_a_chave_ligada_fica_ligada()
    {
        var log = new LogColetor();

        Assert.True(Ler("true", "Development", log));
        Assert.Empty(log.Avisos);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Chave_ligada_fora_de_development_fica_desligada_e_avisa_no_log(string ambiente)
    {
        var log = new LogColetor();

        Assert.False(Ler("true", ambiente, log));
        var aviso = Assert.Single(log.Avisos);
        Assert.Contains(PoliticaDeVelocidadeDeTeste.ChavePermitir, aviso);
        Assert.Contains(ambiente, aviso);
    }

    private static bool Ler(string? valor, string ambiente, ILogger log)
    {
        var valores = new Dictionary<string, string?>();
        if (valor != null)
        {
            valores[PoliticaDeVelocidadeDeTeste.ChavePermitir] = valor;
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return PoliticaDeVelocidadeDeTeste.Ligada(configuracao, new Ambiente(ambiente), log);
    }

    private sealed class Ambiente(string nome) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nome;
        public string ApplicationName { get; set; } = "Teste";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class LogColetor : ILogger
    {
        public List<string> Avisos { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (logLevel == LogLevel.Warning)
            {
                Avisos.Add(formatter(state, exception));
            }
        }
    }
}
