using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>A consulta de posição do Mundo, respondida pelo estado da cidade.</summary>
internal sealed class PosicoesDaCidade(CidadeDoMundo<SessaoTempoReal> cidade) : IPosicoesDoMundo
{
    public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
    {
        var avatar = cidade.Obter(personagemId);
        if (avatar == null)
        {
            posicao = default;
            return false;
        }

        var p = avatar.Posicao;
        posicao = new PosicaoNoMundo(AreasDoMundo.Cidade, p.X, p.Y, p.Z, p.RotacaoY);
        return true;
    }

    public IReadOnlyList<long> PersonagensNaArea(string area) =>
        area == AreasDoMundo.Cidade
            ? cidade.Presentes().Select(avatar => avatar.PersonagemId).ToArray()
            : [];
}
