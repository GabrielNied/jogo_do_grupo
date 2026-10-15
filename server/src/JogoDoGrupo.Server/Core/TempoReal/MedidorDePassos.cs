using System.Diagnostics;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Guarda a duração e o início dos últimos <see cref="Janela"/> passos do laço do tempo real (o
/// "tick") para as métricas. Só o laço registra; qualquer thread pode resumir.
/// </summary>
public sealed class MedidorDePassos
{
    /// <summary>Passos considerados no resumo (uns 5 a 8 s com o laço em dia).</summary>
    public const int Janela = 512;

    private readonly double[] _duracoesMs = new double[Janela];
    private readonly long[] _inicios = new long[Janela];
    private readonly object _trava = new();
    private int _proximo;
    private int _quantidade;

    /// <summary>Registra um passo que começou em <paramref name="inicio"/> (<see cref="Stopwatch"/>).</summary>
    public void Registrar(long inicio, TimeSpan duracao)
    {
        lock (_trava)
        {
            _duracoesMs[_proximo] = duracao.TotalMilliseconds;
            _inicios[_proximo] = inicio;
            _proximo = (_proximo + 1) % Janela;
            _quantidade = Math.Min(_quantidade + 1, Janela);
        }
    }

    public ResumoDosPassos Resumir() => Resumir(Stopwatch.GetTimestamp());

    /// <summary>Resumo visto no instante <paramref name="agora"/> (<see cref="Stopwatch"/>).</summary>
    public ResumoDosPassos Resumir(long agora)
    {
        double[] duracoes;
        long primeiro;
        long ultimo;
        lock (_trava)
        {
            if (_quantidade == 0)
            {
                return new ResumoDosPassos(0, 0, 0, 0, 0);
            }

            duracoes = new double[_quantidade];
            var inicioDaJanela = (_proximo - _quantidade + Janela) % Janela;
            for (var i = 0; i < _quantidade; i++)
            {
                duracoes[i] = _duracoesMs[(inicioDaJanela + i) % Janela];
            }

            primeiro = _inicios[inicioDaJanela];
            ultimo = _inicios[(_proximo - 1 + Janela) % Janela];
        }

        Array.Sort(duracoes);
        var segundos = Stopwatch.GetElapsedTime(primeiro, ultimo).TotalSeconds;
        var porSegundo = segundos > 0 ? (duracoes.Length - 1) / segundos : 0;
        return new ResumoDosPassos(
            Math.Round(porSegundo, 1),
            Math.Round(Percentil(duracoes, 0.50), 3),
            Math.Round(Percentil(duracoes, 0.95), 3),
            Math.Round(duracoes[^1], 3),
            Math.Round(Stopwatch.GetElapsedTime(ultimo, agora).TotalMilliseconds, 1)
        );
    }

    private static double Percentil(double[] ordenados, double p) =>
        ordenados[(int)Math.Ceiling(p * ordenados.Length) - 1];
}

/// <summary>
/// Ritmo e custo do laço do tempo real: passos por segundo (o laço tenta um a cada 10 ms) e a
/// duração de cada passo. Duração alta ou ritmo caindo indica que o servidor não dá conta.
/// <see cref="MsDesdeOUltimoPasso"/> alto mostra o laço travado (a janela guarda só passos já
/// terminados, então um passo preso não aparece nas durações).
/// </summary>
public sealed record ResumoDosPassos(
    double PassosPorSegundo,
    double DuracaoP50Ms,
    double DuracaoP95Ms,
    double DuracaoMaximaMs,
    double MsDesdeOUltimoPasso
);
