using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>As janelas de deslocamento do Mundo, guardadas no estado da cidade.</summary>
internal sealed class DeslocamentosDaCidade(EstadoDasAreasDoMundo<SessaoTempoReal> areas)
    : IDeslocamentosDoMundo
{
    public bool AbrirJanela(long personagemId, double distanciaMetros, double duracaoSegundos) =>
        areas
            .ParaPersonagem(personagemId)
            ?.AbrirJanela(personagemId, distanciaMetros, duracaoSegundos)
        ?? false;
}
