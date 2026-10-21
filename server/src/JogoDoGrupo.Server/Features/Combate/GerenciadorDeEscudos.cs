using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Os escudos de absorção ativos (#170, combate.md). O escudo absorve o dano antes da vida,
/// expira no tempo e não soma: um escudo novo só vale se for mais forte que o que sobrou, e a
/// duração renova. Thread-safe e desacoplado do relógio do sistema.
/// </summary>
public sealed class GerenciadorDeEscudos
{
    private sealed class Escudo
    {
        public int Absorcao { get; set; }
        public int Maxima { get; set; }
        public long ExpiraEmMs { get; set; }
        public long DuracaoTotalMs { get; set; }
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<long, Escudo> _escudos = [];
    private readonly Func<long> _relogioMs;

    /// <summary>Avisa (fora da trava) que o escudo de uma entidade mudou.</summary>
    public event Action<long, EscudoMudou>? Mudou;

    public GerenciadorDeEscudos(Func<long>? relogioMs = null)
    {
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
    }

    /// <summary>Dá um escudo de <paramref name="absorcao"/> por <paramref name="duracaoSegundos"/>.</summary>
    public void Aplicar(long entidadeId, int absorcao, double duracaoSegundos)
    {
        if (absorcao <= 0 || duracaoSegundos <= 0)
        {
            return;
        }

        var agora = _relogioMs();
        var duracaoMs = (long)Math.Round(duracaoSegundos * 1000.0);
        EscudoMudou aviso;
        lock (_trava)
        {
            if (_escudos.TryGetValue(entidadeId, out var atual) && agora < atual.ExpiraEmMs)
            {
                // Mesmo tipo não acumula: fica o mais forte e a duração renova.
                if (absorcao > atual.Absorcao)
                {
                    atual.Absorcao = absorcao;
                    atual.Maxima = absorcao;
                }

                atual.ExpiraEmMs = agora + duracaoMs;
                atual.DuracaoTotalMs = duracaoMs;
            }
            else
            {
                atual = new Escudo
                {
                    Absorcao = absorcao,
                    Maxima = absorcao,
                    ExpiraEmMs = agora + duracaoMs,
                    DuracaoTotalMs = duracaoMs,
                };
                _escudos[entidadeId] = atual;
            }

            aviso = Montar(entidadeId, atual, agora);
        }

        Mudou?.Invoke(entidadeId, aviso);
    }

    /// <summary>
    /// Passa o dano pelo escudo: devolve o que sobra para a vida. Sem escudo ativo, devolve o
    /// dano inteiro.
    /// </summary>
    public int Absorver(long entidadeId, int dano)
    {
        if (dano <= 0)
        {
            return Math.Max(0, dano);
        }

        var agora = _relogioMs();
        EscudoMudou? aviso = null;
        int restante;
        lock (_trava)
        {
            if (!_escudos.TryGetValue(entidadeId, out var escudo) || agora >= escudo.ExpiraEmMs)
            {
                return dano;
            }

            restante = RegrasEscudo.Absorver(escudo.Absorcao, dano, out var sobra);
            escudo.Absorcao = sobra;
            if (sobra <= 0)
            {
                _escudos.Remove(entidadeId);
                aviso = SemEscudo(entidadeId);
            }
            else
            {
                aviso = Montar(entidadeId, escudo, agora);
            }
        }

        Mudou?.Invoke(entidadeId, aviso);
        return restante;
    }

    /// <summary>Quanto o escudo ainda absorve (0 sem escudo ativo).</summary>
    public int AbsorcaoAtual(long entidadeId)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            return _escudos.TryGetValue(entidadeId, out var e) && agora < e.ExpiraEmMs
                ? e.Absorcao
                : 0;
        }
    }

    /// <summary>Tira os escudos que expiraram e avisa cada dono.</summary>
    public void Atualizar()
    {
        var agora = _relogioMs();
        List<long>? expirados = null;
        lock (_trava)
        {
            foreach (var (id, escudo) in _escudos)
            {
                if (agora >= escudo.ExpiraEmMs)
                {
                    (expirados ??= []).Add(id);
                }
            }

            if (expirados != null)
            {
                foreach (var id in expirados)
                {
                    _escudos.Remove(id);
                }
            }
        }

        if (expirados == null)
        {
            return;
        }

        foreach (var id in expirados)
        {
            Mudou?.Invoke(id, SemEscudo(id));
        }
    }

    /// <summary>Esquece o escudo de uma entidade (saiu do jogo), sem avisar.</summary>
    public void Limpar(long entidadeId)
    {
        lock (_trava)
        {
            _escudos.Remove(entidadeId);
        }
    }

    /// <summary>O retrato do escudo ativo, ou falso se não há.</summary>
    public bool ObterRetrato(long entidadeId, out EscudoMudou retrato)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            if (_escudos.TryGetValue(entidadeId, out var e) && agora < e.ExpiraEmMs)
            {
                retrato = Montar(entidadeId, e, agora);
                return true;
            }
        }

        retrato = SemEscudo(entidadeId);
        return false;
    }

    private static EscudoMudou SemEscudo(long entidadeId) => new() { EntidadeId = entidadeId };

    private static EscudoMudou Montar(long entidadeId, Escudo e, long agora) =>
        new()
        {
            EntidadeId = entidadeId,
            Absorcao = e.Absorcao,
            AbsorcaoMaxima = e.Maxima,
            DuracaoRestanteSegundos = Math.Max(0, e.ExpiraEmMs - agora) / 1000f,
            DuracaoTotalSegundos = e.DuracaoTotalMs / 1000f,
        };
}
