using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.TempoReal;
using Serilog.Core;
using Serilog.Events;

namespace JogoDoGrupo.Server.Core.Observabilidade;

/// <summary>Contadores leves do processo, sem guardar dados de jogadores.</summary>
public sealed class MetricasDoServidor
{
    private readonly ConcurrentQueue<DateTimeOffset> _erros = new();
    private ServidorTempoReal? _tempoReal;

    public void Observar(ServidorTempoReal tempoReal) => _tempoReal = tempoReal;

    public void RegistrarErro(DateTimeOffset quando)
    {
        _erros.Enqueue(quando);
        RemoverErrosAntigos(quando);
    }

    public MetricasServidor Obter()
    {
        var agora = DateTimeOffset.UtcNow;
        RemoverErrosAntigos(agora);
        return new MetricasServidor(
            _tempoReal?.Sessoes.Count ?? 0,
            _erros.Count,
            _tempoReal?.Passos.Resumir() ?? new ResumoDosPassos(0, 0, 0, 0, 0)
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
    int ErrosNoUltimoMinuto,
    ResumoDosPassos Passos
);

/// <summary>Mascara segredos e conteúdo de conversa sem perder o evento.</summary>
public sealed class FiltroDadosSensiveis : ILogEventEnricher
{
    private static readonly string[] NomesSensiveis =
    [
        "senha",
        "token",
        "chat",
        "texto",
        "mensagem",
        "textoChat",
        "accessToken",
        "refreshToken",
        "authorization",
        "password",
    ];

    private static bool Sensivel(string nome) =>
        NomesSensiveis.Contains(nome, StringComparer.OrdinalIgnoreCase);

    public void Enrich(LogEvent evento, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var propriedade in evento.Properties.ToArray())
        {
            evento.AddOrUpdateProperty(
                new LogEventProperty(
                    propriedade.Key,
                    Sensivel(propriedade.Key) ? new ScalarValue("***") : Mascarar(propriedade.Value)
                )
            );
        }
    }

    private static LogEventPropertyValue Mascarar(LogEventPropertyValue valor) =>
        valor switch
        {
            StructureValue estrutura => new StructureValue(
                estrutura.Properties.Select(p => new LogEventProperty(
                    p.Name,
                    Sensivel(p.Name) ? new ScalarValue("***") : Mascarar(p.Value)
                )),
                estrutura.TypeTag
            ),
            SequenceValue sequencia => new SequenceValue(sequencia.Elements.Select(Mascarar)),
            DictionaryValue dicionario => new DictionaryValue(
                dicionario.Elements.Select(p => new KeyValuePair<
                    ScalarValue,
                    LogEventPropertyValue
                >(
                    p.Key,
                    p.Key.Value is string nome && Sensivel(nome)
                        ? new ScalarValue("***")
                        : Mascarar(p.Value)
                ))
            ),
            _ => valor,
        };
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
