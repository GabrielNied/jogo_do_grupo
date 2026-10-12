using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Desenvolvimento;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Tests.Features.Desenvolvimento;

public class PoliticaDeComandosTestes
{
    [Fact]
    public async Task Chave_desligada_nao_responde_e_a_posicao_nao_muda()
    {
        var (modulo, teleporte, _) = Montar(ligada: false, "Development");

        await modulo.Pedir(1, 10, "tp 1 0 -20");

        Assert.Equal(0, teleporte.Chamadas);
    }

    [Fact]
    public async Task Producao_com_a_chave_ligada_fica_desligada_e_avisa_no_log()
    {
        var (modulo, teleporte, log) = Montar(ligada: true, "Production");

        await modulo.Pedir(1, 10, "tp 1 0 -20");

        Assert.Equal(0, teleporte.Chamadas);
        Assert.Contains(log.Avisos, a => a.Contains("desligados"));
    }

    [Fact]
    public async Task Development_com_a_chave_ligada_executa_e_registra_o_comando_no_log()
    {
        var (modulo, teleporte, log) = Montar(ligada: true, "Development");

        await modulo.Pedir(7, 10, "tp 1 0 -20");

        Assert.Equal(1, teleporte.Chamadas);
        Assert.Contains(log.Infos, i => i.Contains("conta 7") && i.Contains("personagem 10"));
    }

    [Fact]
    public async Task Conta_fora_da_lista_e_ignorada()
    {
        var (modulo, teleporte, log) = Montar(true, "Development", contas: ["5", "6"]);

        await modulo.Pedir(7, 10, "tp 1 0 -20");

        Assert.Equal(0, teleporte.Chamadas);
        Assert.Contains(log.Avisos, a => a.Contains("conta 7"));
    }

    [Fact]
    public async Task Conta_na_lista_funciona()
    {
        var (modulo, teleporte, _) = Montar(true, "Development", contas: ["5", "6"]);

        await modulo.Pedir(6, 10, "tp 1 0 -20");

        Assert.Equal(1, teleporte.Chamadas);
    }

    private static (DesenvolvimentoModule, TeleporteContador, LogColetor) Montar(
        bool ligada,
        string ambiente,
        string[]? contas = null
    )
    {
        var valores = new Dictionary<string, string?>
        {
            [PoliticaDeComandos.ChavePermitirComandos] = ligada ? "true" : "false",
        };
        for (var i = 0; i < (contas?.Length ?? 0); i++)
        {
            valores[$"{PoliticaDeComandos.ChaveContas}:{i}"] = contas![i];
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var log = new LogColetor();
        var politica = PoliticaDeComandos.Ler(configuracao, new Ambiente(ambiente), log);
        var teleporte = new TeleporteContador();
        var comandos = new ComandosDoServidor(new ExperienciaNula(), teleporte, new SpawnNulo());
        var modulo = new DesenvolvimentoModule();
        modulo.Configurar(politica, comandos, null, log);
        return (modulo, teleporte, log);
    }

    private sealed class Ambiente(string nome) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nome;
        public string ApplicationName { get; set; } = "Teste";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TeleporteContador : ITeleporteDeTeste
    {
        public int Chamadas { get; private set; }

        public (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z)
        {
            Chamadas++;
            return (true, "ok");
        }
    }

    private sealed class ExperienciaNula : IAjusteDeExperienciaDeTeste
    {
        public Task<(bool Aceito, string Texto)> AjustarAsync(
            long personagemId,
            long quantidade,
            CancellationToken cancelamento
        ) => Task.FromResult((true, "ok"));
    }

    private sealed class SpawnNulo : ISpawnDeTeste
    {
        public (bool Aceito, string Texto) SpawnarPerto(long personagemId) => (true, "ok");
    }

    private sealed class LogColetor : ILogger
    {
        public List<string> Avisos { get; } = [];
        public List<string> Infos { get; } = [];

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
            var texto = formatter(state, exception);
            if (logLevel == LogLevel.Warning)
            {
                Avisos.Add(texto);
            }
            else if (logLevel == LogLevel.Information)
            {
                Infos.Add(texto);
            }
        }
    }
}
