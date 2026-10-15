namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Quem passou a se ver e quem deixou de se ver por causa de um movimento.</summary>
public readonly record struct MudancaDeInteresse(long[] Entraram, long[] Sairam)
{
    public static readonly MudancaDeInteresse Nenhuma = new([], []);
}

/// <summary>
/// Área de interesse da cidade (#500): uma grade espacial de células quadradas que diz quem está
/// perto de quem. Dois personagens passam a se ver a até <see cref="RaioDeVisao"/> e só deixam
/// de se ver além de <see cref="RaioDeSaida"/> (a folga evita piscar na borda). A relação é
/// simétrica. Só vale no plano horizontal. Thread-safe.
/// </summary>
public sealed class GradeDeInteresse
{
    /// <summary>Lado de cada célula da grade, em metros.</summary>
    public const float TamanhoDaCelula = 100f;

    /// <summary>Distância (m) em que um personagem passa a ver o outro.</summary>
    public const float RaioDeVisao = 150f;

    /// <summary>Distância (m) acima da qual um personagem deixa de ver o outro.</summary>
    public const float RaioDeSaida = 180f;

    private readonly Lock _trava = new();
    private readonly Dictionary<long, (float X, float Z)> _posicoes = [];
    private readonly Dictionary<(int, int), HashSet<long>> _celulas = [];
    private readonly Dictionary<long, HashSet<long>> _vistos = [];

    /// <summary>Põe ou move o personagem e devolve as relações que mudaram.</summary>
    public MudancaDeInteresse Atualizar(long id, float x, float z)
    {
        lock (_trava)
        {
            if (_posicoes.TryGetValue(id, out var antiga))
            {
                var celulaAntiga = Celula(antiga.X, antiga.Z);
                if (celulaAntiga != Celula(x, z))
                {
                    TirarDaCelula(id, celulaAntiga);
                    _celulas.GetOrAdd(Celula(x, z)).Add(id);
                }
            }
            else
            {
                _celulas.GetOrAdd(Celula(x, z)).Add(id);
                _vistos[id] = [];
            }

            _posicoes[id] = (x, z);
            var meus = _vistos[id];
            var sairam = new List<long>();
            foreach (var outro in meus)
            {
                var p = _posicoes[outro];
                if (Distancia(x, z, p.X, p.Z) > RaioDeSaida)
                {
                    sairam.Add(outro);
                }
            }

            foreach (var outro in sairam)
            {
                meus.Remove(outro);
                _vistos[outro].Remove(id);
            }

            var entraram = new List<long>();
            foreach (var outro in Candidatos(x, z, RaioDeVisao, id))
            {
                if (!meus.Contains(outro))
                {
                    meus.Add(outro);
                    _vistos[outro].Add(id);
                    entraram.Add(outro);
                }
            }

            return new MudancaDeInteresse([.. entraram], [.. sairam]);
        }
    }

    /// <summary>Tira o personagem e devolve quem o via (esses precisam receber a saída).</summary>
    public long[] Remover(long id)
    {
        lock (_trava)
        {
            if (!_posicoes.Remove(id, out var posicao))
            {
                return [];
            }

            TirarDaCelula(id, Celula(posicao.X, posicao.Z));
            var viam = _vistos.Remove(id, out var meus) ? meus.ToArray() : [];
            foreach (var outro in viam)
            {
                _vistos[outro].Remove(id);
            }

            return viam;
        }
    }

    /// <summary>Quem o personagem vê agora (cópia).</summary>
    public HashSet<long> Vistos(long id)
    {
        lock (_trava)
        {
            return _vistos.TryGetValue(id, out var meus) ? [.. meus] : [];
        }
    }

    /// <summary>Os personagens a até <paramref name="raio"/> metros do ponto.</summary>
    public long[] Perto(float x, float z, float raio)
    {
        lock (_trava)
        {
            return [.. Candidatos(x, z, raio, semId: null)];
        }
    }

    private IEnumerable<long> Candidatos(float x, float z, float raio, long? semId)
    {
        var (minX, minZ) = Celula(x - raio, z - raio);
        var (maxX, maxZ) = Celula(x + raio, z + raio);
        for (var cx = minX; cx <= maxX; cx++)
        {
            for (var cz = minZ; cz <= maxZ; cz++)
            {
                if (!_celulas.TryGetValue((cx, cz), out var ids))
                {
                    continue;
                }

                foreach (var id in ids)
                {
                    var p = _posicoes[id];
                    if (id != semId && Distancia(x, z, p.X, p.Z) <= raio)
                    {
                        yield return id;
                    }
                }
            }
        }
    }

    private void TirarDaCelula(long id, (int, int) celula)
    {
        if (_celulas.TryGetValue(celula, out var ids) && ids.Remove(id) && ids.Count == 0)
        {
            _celulas.Remove(celula);
        }
    }

    private static (int, int) Celula(float x, float z) =>
        ((int)MathF.Floor(x / TamanhoDaCelula), (int)MathF.Floor(z / TamanhoDaCelula));

    private static float Distancia(float ax, float az, float bx, float bz)
    {
        var dx = ax - bx;
        var dz = az - bz;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}

internal static class DicionarioExtensoes
{
    public static HashSet<long> GetOrAdd(
        this Dictionary<(int, int), HashSet<long>> dicionario,
        (int, int) chave
    )
    {
        if (!dicionario.TryGetValue(chave, out var valor))
        {
            valor = [];
            dicionario[chave] = valor;
        }

        return valor;
    }
}
