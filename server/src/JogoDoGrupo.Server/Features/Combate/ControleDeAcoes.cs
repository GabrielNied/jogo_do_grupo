using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>O que o client pediu num <c>IntencaoDeAtaque</c>, já validado.</summary>
public readonly record struct PedidoDeAtaque(TipoDeAtaque Tipo, float DirecaoY, int Sequencia);

/// <summary>
/// Um ataque em andamento: preparo, momento de acerto e recuperação, medidos pelo relógio do
/// servidor (ms). Tempos vêm de <see cref="RegrasAtaque"/>.
/// </summary>
public sealed record AcaoDeAtaque(
    long Id,
    PedidoDeAtaque Pedido,
    long InicioMs,
    int Passo = 0,
    long? CortadaEmMs = null,
    bool Interrompida = false
)
{
    public long PreparoMs => Milissegundos(RegrasAtaque.PreparoSegundos(Pedido.Tipo));

    /// <summary>Quando o golpe acerta (fim do preparo), no mesmo relógio do começo.</summary>
    public long AcertoMs => InicioMs + PreparoMs;

    /// <summary>
    /// Quando o personagem volta a poder agir: o fim da recuperação, ou o instante em que uma
    /// esquiva cortou o que faltava dela.
    /// </summary>
    public long FimMs =>
        CortadaEmMs ?? InicioMs + Milissegundos(RegrasAtaque.DuracaoSegundos(Pedido.Tipo));

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

            if (EstaOcupado(estado, agoraMs))
            {
                // Em outra ação (esquiva, reação ao acerto, guarda): não ataca nem entra na fila.
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

            var atual = estado.Atual;
            var emAtaque = atual != null && agoraMs < atual.FimMs;

            // Preparo e momento de acerto são compromisso: a esquiva só vale na recuperação.
            if ((emAtaque && agoraMs < atual!.AcertoMs) || EstaOcupado(estado, agoraMs))
            {
                ocupacaoId = 0;
                return false;
            }

            estado.AcaoCortada = null;
            if (emAtaque)
            {
                // Na recuperação a esquiva corta o que falta dela (e o pedido da fila).
                estado.AcaoCortada = atual;
                estado.Atual = atual! with { CortadaEmMs = agoraMs };
                estado.Fila = null;
            }

            estado.OcupacaoId = ++_proximoId;
            estado.OcupadoAteMs = agoraMs + Math.Max(0, duracaoMs);
            estado.InvulneravelAteMs =
                agoraMs
                + (long)Math.Round(RegrasEsquiva.JanelaDeInvulnerabilidadeSegundos * 1000.0);
            ocupacaoId = estado.OcupacaoId;
            return true;
        }
    }

    /// <summary>
    /// A reserva <paramref name="ocupacaoId"/> não valeu (sem vigor, caído): libera o personagem
    /// na hora, tira a invulnerabilidade e devolve a recuperação do ataque que ela havia cortado.
    /// Não faz nada se a reserva já é outra ou o personagem foi esquecido.
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
                estado.InvulneravelAteMs = 0;
                if (estado.AcaoCortada is { } original && estado.Atual?.Id == original.Id)
                {
                    estado.Atual = original;
                }

                estado.AcaoCortada = null;
            }
        }
    }

    /// <summary>
    /// Se o dano direto some neste instante: nos primeiros
    /// <see cref="RegrasEsquiva.JanelaDeInvulnerabilidadeSegundos"/> da esquiva em andamento.
    /// </summary>
    public bool EstaInvulneravel(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var estado)
                && agoraMs < estado.InvulneravelAteMs;
        }
    }

    /// <summary>
    /// Um golpe confirmado acertou o personagem (dano que entrou). Se não está na folga de uma
    /// reação anterior, nem no pesado em andamento, nem no meio da esquiva, ele é interrompido:
    /// o golpe em curso para (no preparo, nunca chega ao contato), o pedido da fila some, a guarda
    /// cai, e esquiva e ataque ficam recusados por
    /// <see cref="RegrasReacao.AtordoamentoNoAcertoMs"/>. Outro acerto durante a reação ou os
    /// <see cref="RegrasReacao.FolgaDepoisDoAcertoMs"/> seguintes não renova nem interrompe.
    /// Verdadeiro se a reação começou.
    /// </summary>
    public bool TentarReagir(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (!_estados.TryGetValue(personagemId, out var estado))
            {
                estado = new Estado();
                _estados[personagemId] = estado;
            }

            if (
                !RegrasReacao.PodeReagir(agoraMs, estado.FolgaAteMs)
                || agoraMs < estado.OcupadoAteMs // A esquiva em curso termina.
            )
            {
                return false;
            }

            var atual = estado.Atual;
            if (atual != null && agoraMs < atual.FimMs)
            {
                if (atual.Pedido.Tipo == TipoDeAtaque.Pesado)
                {
                    return false; // O pesado em andamento não é interrompido.
                }

                estado.Atual = atual with
                {
                    CortadaEmMs = agoraMs,
                    Interrompida = agoraMs < atual.AcertoMs,
                };
            }

            estado.Fila = null;
            estado.Guardando = false;
            estado.ReagindoAteMs = RegrasReacao.FimDoAtordoamentoMs(agoraMs);
            estado.FolgaAteMs = RegrasReacao.FimDaFolgaMs(agoraMs);
            return true;
        }
    }

    /// <summary>Se o ataque <paramref name="acaoId"/> foi interrompido antes do contato.</summary>
    public bool FoiInterrompida(long personagemId, long acaoId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var estado)
                && estado.Atual is { } atual
                && atual.Id == acaoId
                && atual.Interrompida;
        }
    }

    /// <summary>
    /// Sobe a guarda. Recusa no preparo e no momento de acerto de um golpe, durante a esquiva e
    /// a reação ao acerto. Na recuperação do golpe, segurar a guarda corta o que falta dela (e o
    /// pedido da fila). Falso se recusou; já segurando conta como verdadeiro.
    /// </summary>
    public bool TentarGuardar(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (!_estados.TryGetValue(personagemId, out var estado))
            {
                estado = new Estado();
                _estados[personagemId] = estado;
            }

            if (estado.Guardando)
            {
                return true;
            }

            var atual = estado.Atual;
            var emAtaque = atual != null && agoraMs < atual.FimMs;
            if (
                (emAtaque && agoraMs < atual!.AcertoMs)
                || agoraMs < estado.OcupadoAteMs
                || agoraMs < estado.ReagindoAteMs
            )
            {
                return false;
            }

            if (emAtaque)
            {
                estado.Atual = atual! with { CortadaEmMs = agoraMs };
                estado.Fila = null;
            }

            estado.Guardando = true;
            return true;
        }
    }

    /// <summary>Solta a guarda. Falso se ela já estava solta.</summary>
    public bool SoltarGuarda(long personagemId)
    {
        lock (_trava)
        {
            if (_estados.TryGetValue(personagemId, out var estado) && estado.Guardando)
            {
                estado.Guardando = false;
                return true;
            }

            return false;
        }
    }

    /// <summary>Se a guarda está segurada.</summary>
    public bool EstaGuardando(long personagemId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var estado) && estado.Guardando;
        }
    }

    /// <summary>A ação em curso (ou a última, se já acabou); nulo se o personagem não tem nenhuma.</summary>
    public AcaoDeAtaque? AcaoAtual(long personagemId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var estado) ? estado.Atual : null;
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

    // Chamar com a trava: esquiva, reação ao acerto ou guarda impedem ataque e esquiva.
    private static bool EstaOcupado(Estado estado, long agoraMs) =>
        agoraMs < estado.OcupadoAteMs || agoraMs < estado.ReagindoAteMs || estado.Guardando;

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
            // A ação não valeu: a cadeia fica como estava antes dela.
            estado.ProximoPasso = estado.ProximoPassoAntes;
            estado.UltimoFimMs = estado.UltimoFimAntesMs;
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
        // A cadeia de leves: o passo sobe quando a ação começa (errar o alvo não quebra) e volta
        // ao primeiro se o golpe novo demorou mais que a janela depois do fim da ação anterior.
        estado.ProximoPassoAntes = estado.ProximoPasso;
        var fimAnteriorMs = estado.Atual?.FimMs ?? estado.UltimoFimMs;
        var passo = RegrasAtaque.PassoDoGolpe(
            pedido.Tipo,
            estado.ProximoPasso,
            (agoraMs - fimAnteriorMs) / 1000.0
        );
        estado.UltimoFimAntesMs = fimAnteriorMs;
        estado.UltimoFimMs = fimAnteriorMs;
        estado.ProximoPasso = RegrasAtaque.ProximoPasso(pedido.Tipo, passo);

        estado.Atual = new AcaoDeAtaque(++_proximoId, pedido, agoraMs, passo);
        return estado.Atual;
    }

    private sealed class Estado
    {
        public AcaoDeAtaque? Atual;
        public AcaoDeAtaque? AcaoCortada;
        public PedidoDeAtaque? Fila;
        public int ProximoPasso;
        public int ProximoPassoAntes;
        public long UltimoFimMs = long.MinValue / 2;
        public long UltimoFimAntesMs = long.MinValue / 2;
        public long InvulneravelAteMs;
        public long OcupadoAteMs;
        public long OcupacaoId;
        public long ReagindoAteMs;
        public long FolgaAteMs;
        public bool Guardando;
    }
}
