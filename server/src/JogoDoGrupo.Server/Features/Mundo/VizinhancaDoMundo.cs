using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>A consulta de vizinhança: a grade na cidade, a área inteira nas instâncias.</summary>
internal sealed class VizinhancaDoMundo<TSessao>(
    EstadoDasAreasDoMundo<TSessao> areas,
    GradeDeInteresse grade
) : IVizinhanca
    where TSessao : class
{
    public IReadOnlyCollection<long> QuemVe(long personagemId)
    {
        if (areas.Obter(personagemId) is not { } atual)
        {
            return [];
        }

        return atual.Area == AreasDoMundo.Cidade
            ? grade.Vistos(personagemId)
            : areas.PersonagensNaArea(atual.Area).Where(id => id != personagemId).ToArray();
    }

    public IReadOnlyCollection<long> PersonagensPerto(
        string area,
        float x,
        float z,
        float raioMetros
    )
    {
        if (area == AreasDoMundo.Cidade)
        {
            return grade.Perto(x, z, raioMetros);
        }

        var perto = new List<long>();
        foreach (var id in areas.PersonagensNaArea(area))
        {
            if (areas.Obter(id) is { } a && a.Area == area)
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
