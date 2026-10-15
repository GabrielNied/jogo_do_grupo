using System.Collections.Concurrent;

namespace JogoDoGrupo.Robos;

/// <summary>Tipos de erro que a carga conta. Qualquer um faz a carga falhar.</summary>
public enum TipoDeErro
{
    /// <summary>Registrar, logar ou criar o personagem pela API falhou.</summary>
    Api,

    /// <summary>A conexão UDP não abriu ou a autenticação foi recusada.</summary>
    Conexao,

    /// <summary>Entrou no tempo real mas não recebeu o <c>EstadoDaCidade</c>.</summary>
    EntradaNaCidade,

    /// <summary>O servidor derrubou o robô durante a medição.</summary>
    Queda,

    /// <summary>Ataque sem <c>AtaqueIniciado</c> nem <c>AtaqueRecusado</c> no prazo.</summary>
    AtaqueSemResposta,

    /// <summary>O servidor corrigiu a posição: recusou um movimento honesto.</summary>
    PosicaoForcada,

    /// <summary>Pacote que não dá para ler (sem cabeçalho ou com JSON inválido).</summary>
    PacoteInvalido,
}

/// <summary>Contadores dos robôs, seguros entre threads. Só conta tráfego durante a medição.</summary>
public sealed class ColetorDeMetricas
{
    private readonly ConcurrentQueue<double> _latenciasMs = new();
    private readonly ConcurrentDictionary<TipoDeErro, int> _erros = new();
    private readonly ConcurrentDictionary<string, byte> _exemplosDeErro = new();
    private long _enviadas;
    private long _recebidas;
    private long _bytesRecebidos;
    private long _replicas;
    private long _ataquesRecusados;
    private volatile bool _medindo;

    public bool Medindo
    {
        get => _medindo;
        set => _medindo = value;
    }

    public void Enviou()
    {
        if (_medindo)
        {
            Interlocked.Increment(ref _enviadas);
        }
    }

    public void Recebeu(int bytes)
    {
        if (_medindo)
        {
            Interlocked.Increment(ref _recebidas);
            Interlocked.Add(ref _bytesRecebidos, bytes);
        }
    }

    public void RecebeuReplica()
    {
        if (_medindo)
        {
            Interlocked.Increment(ref _replicas);
        }
    }

    public void Latencia(double ms, bool recusado)
    {
        if (!_medindo)
        {
            return;
        }

        _latenciasMs.Enqueue(ms);
        if (recusado)
        {
            Interlocked.Increment(ref _ataquesRecusados);
        }
    }

    /// <summary>Conta o erro sempre (inclusive na entrada) e guarda até 5 exemplos de texto.</summary>
    public void Erro(TipoDeErro tipo, string detalhe)
    {
        _erros.AddOrUpdate(tipo, 1, (_, n) => n + 1);
        if (_exemplosDeErro.Count < 5)
        {
            _exemplosDeErro.TryAdd($"{tipo}: {detalhe}", 0);
        }
    }

    public int TotalDeErros => _erros.Values.Sum();

    public RelatorioDeCarga Relatorio(
        OpcoesDeCarga opcoes,
        int robosNoMundo,
        double segundos,
        TickDoServidor? servidor
    )
    {
        var latencias = _latenciasMs.ToArray();
        Array.Sort(latencias);
        var porSegundo = segundos > 0 ? 1 / segundos : 0;
        return new RelatorioDeCarga
        {
            Robos = opcoes.Robos,
            RobosNoMundo = robosNoMundo,
            SegundosMedidos = Math.Round(segundos, 2),
            Enviadas = _enviadas,
            Recebidas = _recebidas,
            EnviadasPorSegundo = Math.Round(_enviadas * porSegundo, 1),
            RecebidasPorSegundo = Math.Round(_recebidas * porSegundo, 1),
            KbRecebidosPorSegundo = Math.Round(_bytesRecebidos * porSegundo / 1024, 1),
            ReplicasPorSegundo = Math.Round(_replicas * porSegundo, 1),
            Latencia = new LatenciaDoAtaque
            {
                Amostras = latencias.Length,
                Recusados = _ataquesRecusados,
                P50Ms = Percentil(latencias, 0.50),
                P95Ms = Percentil(latencias, 0.95),
                P99Ms = Percentil(latencias, 0.99),
                MaximaMs = latencias.Length > 0 ? Math.Round(latencias[^1], 1) : 0,
            },
            Servidor = servidor,
            Erros = Enum.GetValues<TipoDeErro>()
                .Where(t => _erros.ContainsKey(t))
                .ToDictionary(t => t.ToString(), t => _erros[t]),
            ExemplosDeErro = _exemplosDeErro.Keys.Order().ToArray(),
        };
    }

    private static double Percentil(double[] ordenados, double p) =>
        ordenados.Length == 0
            ? 0
            : Math.Round(ordenados[(int)Math.Ceiling(p * ordenados.Length) - 1], 1);
}
