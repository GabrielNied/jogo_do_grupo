using System.Collections.Concurrent;
using System.Security.Cryptography;
using JogoDoGrupo.Shared.Core.Contas;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>Regras da recuperação de conta por e-mail (#1167).</summary>
public static class RecuperacaoDeConta
{
    /// <summary>Tentativas erradas com um código antes de ele cair (exige um pedido novo).</summary>
    public const int TentativasPorCodigo = 5;

    /// <summary>Pedidos de código por conta numa hora; acima disso a resposta é a mesma e nada sai.</summary>
    public const int PedidosPorHoraPorContaPadrao = 3;

    public static readonly TimeSpan ValidadeCodigo = TimeSpan.FromMinutes(
        RegrasConta.CodigoRecuperacaoMinutos
    );

    /// <summary>Código aleatório de <see cref="RegrasConta.CodigoRecuperacaoDigitos"/> dígitos.</summary>
    public static string GerarCodigo()
    {
        var maximo = (int)Math.Pow(10, RegrasConta.CodigoRecuperacaoDigitos);
        return RandomNumberGenerator
            .GetInt32(0, maximo)
            .ToString($"D{RegrasConta.CodigoRecuperacaoDigitos}");
    }

    public static string AssuntoEmail => "Código para redefinir a senha";

    public static string TextoEmail(string usuario, string codigo) =>
        $"Olá, {usuario}.\n\n"
        + $"Seu código para redefinir a senha no Jogo do Grupo é {codigo}. "
        + $"Ele vale {RegrasConta.CodigoRecuperacaoMinutos} minutos e serve uma vez só.\n\n"
        + "Se não foi você quem pediu, ignore este e-mail: sua senha continua a mesma.";
}

/// <summary>
/// Limita os pedidos de código por conta numa janela de uma hora (em memória, como o
/// <see cref="ControleDeFalhasLogin"/>). O limite por endereço IP fica no rate limiter.
/// </summary>
public sealed class ControleDePedidosRecuperacao(TimeProvider relogio, int limitePorHora)
{
    public static readonly TimeSpan Janela = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<long, Queue<DateTimeOffset>> _pedidos = new();

    /// <summary>Registra um pedido da conta; <c>false</c> se ela já chegou ao limite da hora.</summary>
    public bool TentarRegistrar(long contaId)
    {
        var agora = relogio.GetUtcNow();
        var fila = _pedidos.GetOrAdd(contaId, _ => new Queue<DateTimeOffset>());
        lock (fila)
        {
            while (fila.Count > 0 && agora - fila.Peek() >= Janela)
            {
                fila.Dequeue();
            }

            if (fila.Count >= limitePorHora)
            {
                return false;
            }

            fila.Enqueue(agora);
            return true;
        }
    }
}
