using System.Collections.Concurrent;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Controla falhas consecutivas de login por usuário, aplicando atraso progressivo e bloqueio temporário (#517).
/// </summary>
public sealed class ControleDeFalhasLogin(TimeProvider relogio)
{
    public const int FalhasParaBloqueio = 5;
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan JanelaDeInatividade = TimeSpan.FromMinutes(15);

    private sealed class RegistroDeFalhas
    {
        public int Tentativas { get; set; }
        public DateTimeOffset? BloqueadoAte { get; set; }
        public DateTimeOffset UltimaTentativa { get; set; }
    }

    private readonly ConcurrentDictionary<string, RegistroDeFalhas> _falhas = new();

    /// <summary>Verifica se o usuário está temporariamente bloqueado por excesso de falhas.</summary>
    public bool EstaBloqueado(string usuarioNormalizado, out TimeSpan tempoRestante)
    {
        tempoRestante = TimeSpan.Zero;
        if (!_falhas.TryGetValue(usuarioNormalizado, out var reg))
        {
            return false;
        }

        var agora = relogio.GetUtcNow();
        if (reg.BloqueadoAte.HasValue)
        {
            if (reg.BloqueadoAte.Value > agora)
            {
                tempoRestante = reg.BloqueadoAte.Value - agora;
                return true;
            }

            // Expirou o bloqueio
            reg.BloqueadoAte = null;
            reg.Tentativas = 0;
        }

        return false;
    }

    /// <summary>
    /// Registra uma tentativa falha, calcula atraso progressivo e aciona bloqueio se atingir K falhas.
    /// </summary>
    public async Task RegistrarFalhaAsync(string usuarioNormalizado, CancellationToken ct = default)
    {
        var agora = relogio.GetUtcNow();
        var reg = _falhas.GetOrAdd(
            usuarioNormalizado,
            _ => new RegistroDeFalhas { UltimaTentativa = agora }
        );

        lock (reg)
        {
            // Se passou muito tempo desde a última tentativa, reseta
            if (agora - reg.UltimaTentativa > JanelaDeInatividade)
            {
                reg.Tentativas = 0;
            }

            reg.Tentativas++;
            reg.UltimaTentativa = agora;

            if (reg.Tentativas >= FalhasParaBloqueio)
            {
                reg.BloqueadoAte = agora.Add(DuracaoBloqueio);
            }
        }

        // Atraso progressivo para falhas repetidas antes do bloqueio (ex: 3ª = 500ms, 4ª = 1000ms)
        var atraso = ObterAtrasoProgressivo(reg.Tentativas);
        if (atraso > TimeSpan.Zero)
        {
            await Task.Delay(atraso, relogio, ct);
        }
    }

    /// <summary>Limpa o histórico de falhas ao autenticar com sucesso.</summary>
    public void RegistrarSucesso(string usuarioNormalizado)
    {
        _falhas.TryRemove(usuarioNormalizado, out _);
    }

    private static TimeSpan ObterAtrasoProgressivo(int tentativas) =>
        tentativas switch
        {
            3 => TimeSpan.FromMilliseconds(500),
            4 => TimeSpan.FromMilliseconds(1000),
            _ => TimeSpan.Zero,
        };
}
