using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JogoDoGrupo.Robos;

/// <summary>Resultado de uma carga, em texto (<see cref="Texto"/>) ou JSON (<see cref="ParaJson"/>).</summary>
public sealed record RelatorioDeCarga
{
    public int Robos { get; init; }

    /// <summary>Robôs que entraram na cidade e foram medidos.</summary>
    public int RobosNoMundo { get; init; }

    public double SegundosMedidos { get; init; }

    /// <summary>Pacotes que os robôs mandaram ao servidor.</summary>
    public long Enviadas { get; init; }

    /// <summary>Pacotes que os robôs receberam do servidor.</summary>
    public long Recebidas { get; init; }

    public double EnviadasPorSegundo { get; init; }

    public double RecebidasPorSegundo { get; init; }

    public double KbRecebidosPorSegundo { get; init; }

    /// <summary><c>PosicaoJogador</c> de outros robôs recebidas por segundo (a replicação).</summary>
    public double ReplicasPorSegundo { get; init; }

    public LatenciaDoAtaque Latencia { get; init; } = new();

    /// <summary>Tick do servidor (<c>/admin/metricas</c>) no fim da medição; <c>null</c> sem chave JWT.</summary>
    public TickDoServidor? Servidor { get; init; }

    public Dictionary<string, int> Erros { get; init; } = [];

    public string[] ExemplosDeErro { get; init; } = [];

    public int TotalDeErros => Erros.Values.Sum();

    private static readonly JsonSerializerOptions Formato = new() { WriteIndented = true };

    public string ParaJson() => JsonSerializer.Serialize(this, Formato);

    public string Texto()
    {
        var c = CultureInfo.InvariantCulture;
        var t = new StringBuilder();
        t.AppendLine(c, $"Robôs: {RobosNoMundo}/{Robos} no mundo, {SegundosMedidos:0.#} s medidos");
        t.AppendLine(
            c,
            $"Mensagens: {EnviadasPorSegundo:0.#}/s enviadas, {RecebidasPorSegundo:0.#}/s recebidas ({KbRecebidosPorSegundo:0.#} KB/s), réplicas {ReplicasPorSegundo:0.#}/s"
        );
        t.AppendLine(
            c,
            $"Latência do ataque (ida e volta): p50 {Latencia.P50Ms:0.#} ms, p95 {Latencia.P95Ms:0.#} ms, p99 {Latencia.P99Ms:0.#} ms, máx {Latencia.MaximaMs:0.#} ms ({Latencia.Amostras} amostras, {Latencia.Recusados} recusados)"
        );
        if (Servidor is { } s)
        {
            t.AppendLine(
                c,
                $"Tick do servidor: {s.PassosPorSegundo:0.#} passos/s, duração p50 {s.DuracaoP50Ms:0.###} ms, p95 {s.DuracaoP95Ms:0.###} ms, máx {s.DuracaoMaximaMs:0.###} ms; {s.JogadoresOnline} online, {s.ErrosNoUltimoMinuto} erros no log no último minuto"
            );
        }
        else
        {
            t.AppendLine("Tick do servidor: sem leitura (passe --chave-jwt)");
        }
        t.Append("Erros: ").Append(TotalDeErros);
        if (TotalDeErros > 0)
        {
            t.Append(" (").AppendJoin(", ", Erros.Select(e => $"{e.Key} {e.Value}")).Append(')');
        }
        foreach (var exemplo in ExemplosDeErro)
        {
            t.AppendLine().Append("  ").Append(exemplo);
        }

        return t.ToString();
    }
}

/// <summary>
/// Tempo entre mandar <c>IntencaoDeAtaque</c> e receber o <c>AtaqueIniciado</c> do próprio robô
/// (ou o <c>AtaqueRecusado</c>): passa pelo laço do servidor, pelo Combate e pelo banco.
/// </summary>
public sealed record LatenciaDoAtaque
{
    public int Amostras { get; init; }
    public long Recusados { get; init; }
    public double P50Ms { get; init; }
    public double P95Ms { get; init; }
    public double P99Ms { get; init; }
    public double MaximaMs { get; init; }
}

/// <summary>O que o servidor conta de si (<c>MetricasServidor</c> do Core, sem referenciá-lo).</summary>
public sealed record TickDoServidor
{
    public int JogadoresOnline { get; init; }
    public int ErrosNoUltimoMinuto { get; init; }
    public double PassosPorSegundo { get; init; }
    public double DuracaoP50Ms { get; init; }
    public double DuracaoP95Ms { get; init; }
    public double DuracaoMaximaMs { get; init; }
}
