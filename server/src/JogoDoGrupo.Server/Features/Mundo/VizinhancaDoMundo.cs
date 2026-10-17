using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>A consulta de vizinhança: a grade na cidade, a área inteira nas instâncias.</summary>
internal sealed class VizinhancaDoMundo<TSessao> : IVizinhanca
    where TSessao : class
{
    private readonly EstadoDasAreasDoMundo<TSessao> _areas;
    private readonly Func<string, GradeDeInteresse> _obterGrade;

    public VizinhancaDoMundo(EstadoDasAreasDoMundo<TSessao> areas, GradeDeInteresse grade)
        : this(areas, _ => grade) { }

    public VizinhancaDoMundo(
        EstadoDasAreasDoMundo<TSessao> areas,
        Func<string, GradeDeInteresse> obterGrade
    )
    {
        _areas = areas;
        _obterGrade = obterGrade;
    }

    public IReadOnlyCollection<long> QuemVe(long personagemId)
    {
        if (_areas.Obter(personagemId) is not { } atual)
        {
            return [];
        }

        return AreasDoMundo.EhCidade(atual.Area)
            ? _obterGrade(atual.Area).Vistos(personagemId)
            : _areas.PersonagensNaArea(atual.Area).Where(id => id != personagemId).ToArray();
    }

    public IReadOnlyCollection<long> PersonagensPerto(
        string area,
        float x,
        float z,
        float raioMetros
    )
    {
        if (AreasDoMundo.EhCidade(area))
        {
            return _obterGrade(area).Perto(x, z, raioMetros);
        }

        var perto = new List<long>();
        foreach (var id in _areas.PersonagensNaArea(area))
        {
            if (_areas.Obter(id) is { } a && a.Area == area)
            {
                var dx = a.Avatar.Posicao.X - x;
                var dz = a.Avatar.Posicao.Z - z;
                if (dx * dx + dz * dz <= raioMetros * raioMetros)
                {
                    perto.Add(id);
                }
            }
        }

        return perto;
    }
}
