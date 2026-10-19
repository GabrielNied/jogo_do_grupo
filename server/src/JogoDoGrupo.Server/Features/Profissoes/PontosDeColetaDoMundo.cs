using JogoDoGrupo.Shared.Features.Profissoes;

namespace JogoDoGrupo.Server.Features.Profissoes;

/// <summary>
/// O estado dos pontos de coleta, por área (cada canal da cidade tem os seus). Os pontos são
/// compartilhados: quem coleta primeiro leva e o ponto some para todos até o respawn. A decisão
/// é atômica (uma trava), então dois pedidos no mesmo instante nunca levam o mesmo ponto. O
/// estado fica só na memória: reiniciar o servidor devolve todos os pontos.
/// </summary>
public sealed class PontosDeColetaDoMundo(TimeProvider relogio)
{
    private readonly object _trava = new();
    private readonly Dictionary<(string Area, int NoId), DateTimeOffset> _voltaEm = new();

    /// <summary>Leva o ponto se ele está disponível. Falso se outro já levou.</summary>
    public bool TentarColetar(string area, int noId)
    {
        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            if (_voltaEm.TryGetValue((area, noId), out var volta) && agora < volta)
            {
                return false;
            }

            _voltaEm[(area, noId)] = agora.AddSeconds(ConfigProfissoes.RespawnSegundos);
            return true;
        }
    }

    /// <summary>Desfaz a coleta quando o crédito do material falhou.</summary>
    public void Devolver(string area, int noId)
    {
        lock (_trava)
        {
            _voltaEm.Remove((area, noId));
        }
    }

    /// <summary>Os pontos esgotados da área e os segundos que faltam para cada um voltar.</summary>
    public EstadoDosNos EstadoDaArea(string area)
    {
        var agora = relogio.GetUtcNow();
        var esgotados = new List<int>();
        var segundos = new List<int>();
        lock (_trava)
        {
            foreach (var ponto in ConfigProfissoes.Pontos)
            {
                if (_voltaEm.TryGetValue((area, ponto.Id), out var volta) && agora < volta)
                {
                    esgotados.Add(ponto.Id);
                    segundos.Add((int)Math.Ceiling((volta - agora).TotalSeconds));
                }
            }
        }

        return new EstadoDosNos(esgotados.ToArray(), segundos.ToArray());
    }
}
