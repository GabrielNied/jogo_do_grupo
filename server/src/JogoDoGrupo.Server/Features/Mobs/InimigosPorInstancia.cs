using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Grupos de inimigos comuns criados por instância de dungeon. Implementa
/// <see cref="IInimigosPorInstancia"/> para Instâncias e <see cref="IAlvosDeCombate"/> para o
/// Combate achar os alvos dinâmicos.
/// </summary>
public sealed class InimigosPorInstancia(ISorteadorDeCriaturas sorteador, IEstadoDeQueda queda)
    : IInimigosPorInstancia,
        IAlvosDeCombate
{
    private readonly ConcurrentDictionary<string, GrupoDeInimigos[]> _porArea = new();
    private long _proximoBlocoDeIds = ConfigInimigosDaInstancia.PrimeiroId;

    /// <summary>Todos os grupos ativos, para o laço de IA.</summary>
    public IEnumerable<IInimigosSimulados> Simulados() => _porArea.Values.SelectMany(g => g);

    public void Criar(string areaId, IReadOnlyList<GrupoDeInstancia> pedidos)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaId);
        ArgumentNullException.ThrowIfNull(pedidos);
        Remover(areaId);

        var grupos = new List<GrupoDeInimigos>();
        foreach (var pedido in pedidos)
        {
            var primeiroId =
                Interlocked.Add(ref _proximoBlocoDeIds, ConfigInimigosDaInstancia.IdsPorGrupo)
                - ConfigInimigosDaInstancia.IdsPorGrupo;
            grupos.Add(GrupoDeInimigos.ParaInstancia(areaId, pedido, sorteador, primeiroId, queda));
        }

        _porArea[areaId] = grupos.ToArray();
    }

    public bool Remover(string areaId) => areaId != null && _porArea.TryRemove(areaId, out _);

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        if (!_porArea.TryGetValue(area, out var grupos))
        {
            return [];
        }

        return grupos
            .SelectMany(g =>
                g.AlvosNoVolume(area, origemX, origemZ, direcaoYGraus, instanteReferenciaMs)
            )
            .ToArray();
    }

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        foreach (var grupo in _porArea.Values.SelectMany(g => g))
        {
            if (grupo.TentarAplicarDano(alvoId, dano, atacanteId, out resultado))
            {
                return true;
            }
        }

        resultado = default;
        return false;
    }

    public bool Provocar(long alvoId, long provocadorId)
    {
        foreach (var grupo in _porArea.Values.SelectMany(g => g))
        {
            if (grupo.Provocar(alvoId, provocadorId))
            {
                return true;
            }
        }

        return false;
    }
}
