using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Os bosses de dungeon que existem agora, um por área. Implementa o contrato
/// <see cref="IChefesDaDungeon"/> para a instância e é o <see cref="IAlvosDeCombate"/> deles:
/// o Combate acha o boss pela área do golpe. Seguro para várias threads.
/// </summary>
public sealed class ChefesDaDungeon(
    Func<long>? relogioMs = null,
    IHistoricoDePosicoesDoMundo? historico = null
) : IChefesDaDungeon, IAlvosDeCombate
{
    private readonly ConcurrentDictionary<string, ChefeDaDungeon> _porArea = new();
    private long _ultimoId = ConfigChefeDaDungeon.PrimeiroIdDoChefe - 1;

    public long Criar(string areaId, double x, double z)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaId);
        var chefe = new ChefeDaDungeon(
            areaId,
            x,
            z,
            () => Interlocked.Increment(ref _ultimoId),
            relogioMs,
            historico
        );
        _porArea[areaId] = chefe;
        return chefe.InimigoId;
    }

    public bool Remover(string areaId) => areaId != null && _porArea.TryRemove(areaId, out _);

    /// <summary>O boss da área, ou <c>null</c> se ela não tem.</summary>
    public ChefeDaDungeon? Obter(string areaId) =>
        areaId != null && _porArea.TryGetValue(areaId, out var chefe) ? chefe : null;

    /// <summary>Os bosses que existem agora (cópia: pode mudar depois).</summary>
    public IReadOnlyList<ChefeDaDungeon> Ativos() => _porArea.Values.ToArray();

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    ) =>
        Obter(area)?.AlvosNoVolume(area, origemX, origemZ, direcaoYGraus, instanteReferenciaMs)
        ?? [];

    public IReadOnlyList<AlvoDeCombate> AlvosProximos(
        string area,
        double origemX,
        double origemZ,
        double alcanceMetros,
        long instanteReferenciaMs = 0
    ) =>
        Obter(area)?.AlvosProximos(area, origemX, origemZ, alcanceMetros, instanteReferenciaMs)
        ?? [];

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        foreach (var chefe in _porArea.Values)
        {
            if (chefe.TentarAplicarDano(alvoId, dano, atacanteId, out resultado))
            {
                return true;
            }
        }

        resultado = default;
        return false;
    }

    public bool Provocar(long alvoId, long provocadorId)
    {
        foreach (var chefe in _porArea.Values)
        {
            if (chefe.Provocar(alvoId, provocadorId))
            {
                return true;
            }
        }

        return false;
    }
}
