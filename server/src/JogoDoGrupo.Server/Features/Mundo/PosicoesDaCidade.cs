using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>A consulta de posição do Mundo, respondida pelo estado de cada área.</summary>
internal sealed class PosicoesDoMundo<TSessao>(EstadoDasAreasDoMundo<TSessao> areas)
    : IPosicoesDoMundo
    where TSessao : class
{
    public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
    {
        var localizado = areas.Obter(personagemId);
        if (localizado == null)
        {
            posicao = default;
            return false;
        }

        var (area, avatar) = localizado.Value;
        var p = avatar.Posicao;
        posicao = new PosicaoNoMundo(area, p.X, p.Y, p.Z, p.RotacaoY);
        return true;
    }

    public IReadOnlyList<long> PersonagensNaArea(string area) => areas.PersonagensNaArea(area);
}
