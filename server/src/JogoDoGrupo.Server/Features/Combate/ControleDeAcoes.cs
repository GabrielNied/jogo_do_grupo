using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>O que o client pediu num <c>IntencaoDeAtaque</c>, já validado.</summary>
public readonly record struct PedidoDeAtaque(TipoDeAtaque Tipo, float DirecaoY, int Sequencia);

/// <summary>
/// Um ataque em andamento: preparo, momento de acerto e recuperação, medidos pelo relógio do
/// servidor (ms). Tempos vêm de <see cref="RegrasAtaque"/>.
/// </summary>
public sealed record AcaoDeAtaque(long Id, PedidoDeAtaque Pedido, long InicioMs)
{
    public long PreparoMs => Milissegundos(RegrasAtaque.PreparoSegundos(Pedido.Tipo));

    /// <summary>Quando o personagem volta a poder agir.</summary>
    public long FimMs => InicioMs + Milissegundos(RegrasAtaque.DuracaoSegundos(Pedido.Tipo));

    private static long Milissegundos(double segundos) => (long)Math.Round(segundos * 1000.0);
}

/// <summary>O que aconteceu com um pedido de ataque.</summary>
public enum ResultadoDoPedido
{
    /// <summary>Começou agora: <c>Acao</c> é o novo ataque.</summary>
    Iniciado,

    /// <summary>Ficou na fila de 1 pedido: <c>Acao</c> é o ataque em curso, que ele espera.</summary>
    Enfileirado,

    /// <summary>Não coube (ação em andamento e fila fora da janela ou ocupada).</summary>
    Recusado,
}

/// <summary>
/// Quem está atacando e quando pode agir de novo: uma ação por personagem e uma fila de 1
/// pedido (<see cref="RegrasAtaque.EntraNaFila"/>). Só decide; não mexe em rede, banco nem
/// relógio (o tempo entra como parâmetro, em ms). Seguro para várias threads.
/// </summary>
public sealed class ControleDeAcoes
{
    private readonly Lock _trava = new();
    private readonly Dictionary<long, Estado> _estados = [];
    private long _proximoId;

    /// <summary>
    /// Pede um ataque. Sem ação em curso (ou com ela já terminada) começa na hora; durante uma
    /// ação, entra na fila se faltar no máximo <see cref="RegrasAtaque.JanelaDaFilaSegundos"/> e
    /// a fila estiver vazia; senão é recusado.
    /// </summary>
    public ResultadoDoPedido Pedir(
        long personagemId,
        PedidoDeAtaque pedido,
        long agoraMs,
        out AcaoDeAtaque? acao
    )
    {
        lock (_trava)
        {
            if (!_estados.TryGetValue(personagemId, out var estado))
            {
                estado = new Estado();
                _estados[personagemId] = estado;
            }

            if (agoraMs < estado.OcupadoAteMs)
            {
                // Em outra ação (ex.: esquiva): não ataca nem entra na fila.
                acao = null;
                return ResultadoDoPedido.Recusado;
            }

            var atual = estado.Atual;
            if (atual != null && agoraMs < atual.FimMs)
            {
                var faltaSegundos = (atual.FimMs - agoraMs) / 1000.0;
                if (estado.Fila == null && RegrasAtaque.EntraNaFila(faltaSegundos))
                {
                    estado.Fila = pedido;
                    acao = atual;
                    return ResultadoDoPedido.Enfileirado;
                }

                acao = null;
                return ResultadoDoPedido.Recusado;
            }

            estado.Fila = null;
            acao = Iniciar(estado, pedido, agoraMs);
            return ResultadoDoPedido.Iniciado;
        }
    }

    /// <summary>
    /// A ação <paramref name="acaoId"/> acabou (chamar no <c>FimMs</c>). Se havia um pedido na
    /// fila, ele começa agora e é devolvido. Nulo se a ação já foi trocada ou esquecida.
    /// </summary>
    public AcaoDeAtaque? AvancarFila(long personagemId, long acaoId, long agoraMs)
    {
        lock (_trava)
        {
            return ProximaDaFila(personagemId, acaoId, agoraMs);
        }
    }

    /// <summary>
    /// A ação <paramref name="acaoId"/> não valeu (sem vigor, caído): libera o personagem e,
    /// se havia um pedido na fila, começa ele agora e o devolve.
    /// </summary>
    public AcaoDeAtaque? Desistir(long personagemId, long acaoId, long agoraMs)
    {
        lock (_trava)
        {
            return ProximaDaFila(personagemId, acaoId, agoraMs, liberar: true);
        }
    }

    /// <summary>
    /// Reserva o personagem para uma ação que não é ataque (ex.: a esquiva) por
    /// <paramref name="duracaoMs"/>. Falso se ele está no meio de um ataque ou de outra reserva:
    /// de dois pedidos no mesmo instante, um vale. Durante a reserva, <see cref="Pedir"/> recusa.
    /// </summary>
    public bool TentarOcupar(long personagemId, long agoraMs, long duracaoMs, out long ocupacaoId)
    {
        lock (_trava)
        {
            if (!_estados.TryGetValue(personagemId, out var estado))
            {
                estado = new Estado();
                _estados[personagemId] = estado;
            }

            var emAtaque = estado.Atual != null && agoraMs < estado.Atual.FimMs;
            if (emAtaque || agoraMs < estado.OcupadoAteMs)
            {
                ocupacaoId = 0;
                return false;
            }

            estado.OcupacaoId = ++_proximoId;
            estado.OcupadoAteMs = agoraMs + Math.Max(0, duracaoMs);
            ocupacaoId = estado.OcupacaoId;
            return true;
        }
    }

    /// <summary>
    /// A reserva <paramref name="ocupacaoId"/> não valeu (sem vigor, caído): libera o personagem
    /// na hora. Não faz nada se a reserva já é outra ou o personagem foi esquecido.
    /// </summary>
    public void Desocupar(long personagemId, long ocupacaoId)
    {
        lock (_trava)
        {
            if (
                _estados.TryGetValue(personagemId, out var estado)
                && estado.OcupacaoId == ocupacaoId
            )
            {
                estado.OcupadoAteMs = 0;
            }
        }
    }

    /// <summary>Se a ação ainda é a atual do personagem (não foi trocada nem esquecida).</summary>
    public bool EstaAtiva(long personagemId, long acaoId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var estado) && estado.Atual?.Id == acaoId;
        }
    }

    /// <summary>Esquece o personagem (saiu do jogo): ação e fila somem.</summary>
    public void Esquecer(long personagemId)
    {
        lock (_trava)
        {
            _estados.Remove(personagemId);
        }
    }

    // Chamar com a trava.
    private AcaoDeAtaque? ProximaDaFila(
        long personagemId,
        long acaoId,
        long agoraMs,
        bool liberar = false
    )
    {
        if (!_estados.TryGetValue(personagemId, out var estado) || estado.Atual?.Id != acaoId)
        {
            return null;
        }

        if (liberar)
        {
            estado.Atual = null;
        }

        if (estado.Fila is not { } proximo)
        {
            return null;
        }

        estado.Fila = null;
        return Iniciar(estado, proximo, agoraMs);
    }

    // Chamar com a trava.
    private AcaoDeAtaque Iniciar(Estado estado, PedidoDeAtaque pedido, long agoraMs)
    {
        estado.Atual = new AcaoDeAtaque(++_proximoId, pedido, agoraMs);
        return estado.Atual;
    }

    private sealed class Estado
    {
        public AcaoDeAtaque? Atual;
        public PedidoDeAtaque? Fila;
        public long OcupadoAteMs;
        public long OcupacaoId;
    }
}
