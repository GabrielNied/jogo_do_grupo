using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.TempoReal;
using Serilog.Core;
using Serilog.Events;

namespace JogoDoGrupo.Server.Core.Observabilidade;

/// <summary>Contadores leves do processo, sem guardar dados de jogadores.</summary>
public sealed class MetricasDoServidor
{
    private readonly ConcurrentQueue<DateTimeOffset> _erros = new();
    private long _ouroCriado;
    private long _ouroDestruido;
    private ServidorTempoReal? _tempoReal;

    public void Observar(ServidorTempoReal tempoReal) => _tempoReal = tempoReal;

    public void RegistrarErro(DateTimeOffset quando)
    {
        _erros.Enqueue(quando);
        RemoverErrosAntigos(quando);
    }

    /// <summary>Chamado pela Economia quando uma operação cria ouro.</summary>
    public void RegistrarOuroCriado(long quantidade) =>
        Interlocked.Add(ref _ouroCriado, quantidade);

    /// <summary>Chamado pela Economia quando uma operação remove ouro.</summary>
    public void RegistrarOuroDestruido(long quantidade) =>
        Interlocked.Add(ref _ouroDestruido, quantidade);

    public MetricasServidor Obter()
    {
        var agora = DateTimeOffset.UtcNow;
        RemoverErrosAntigos(agora);
        return new MetricasServidor(
            _tempoReal?.Sessoes.Count ?? 0,
            Interlocked.Read(ref _ouroCriado),
            Interlocked.Read(ref _ouroDestruido),
            _erros.Count
        );
    }

    private void RemoverErrosAntigos(DateTimeOffset agora)
    {
        while (_erros.TryPeek(out var erro) && erro < agora.AddMinutes(-1))
        {
            _erros.TryDequeue(out _);
        }
    }
}

/// <summary>Resposta do comando administrativo de métricas.</summary>
public sealed record MetricasServidor(
    int JogadoresOnline,
    long OuroCriado,
    long OuroDestruido,
    int ErrosNoUltimoMinuto
);

/// <summary>Descarta eventos que tragam segredo ou conteúdo de conversa.</summary>
public static class FiltroDadosSensiveis
{
    private static readonly string[] NomesSensiveis =
    [
        "senha",
        "token",
        "chat",
        "texto",
        "mensagem",
    ];

    public static bool TemDadoSensivel(LogEvent evento) =>
        evento.Properties.Keys.Any(nome =>
            NomesSensiveis.Any(sensivel =>
                nome.Contains(sensivel, StringComparison.OrdinalIgnoreCase)
            )
        );
}

/// <summary>Conta erros do Serilog sem gerar novos logs.</summary>
public sealed class ContadorDeErrosSink(MetricasDoServidor metricas) : ILogEventSink
{
    public void Emit(LogEvent evento)
    {
        if (evento.Level >= LogEventLevel.Error)
        {
            metricas.RegistrarErro(evento.Timestamp);
        }
    }
}
