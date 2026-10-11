using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>As janelas de deslocamento do Mundo, guardadas no estado da cidade.</summary>
internal sealed class DeslocamentosDaCidade(CidadeDoMundo<SessaoTempoReal> cidade)
    : IDeslocamentosDoMundo
{
    public bool AbrirJanela(long personagemId, double distanciaMetros, double duracaoSegundos) =>
        cidade.AbrirJanela(personagemId, distanciaMetros, duracaoSegundos);
}
