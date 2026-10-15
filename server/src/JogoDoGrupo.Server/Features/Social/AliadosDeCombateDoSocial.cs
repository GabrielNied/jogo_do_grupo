using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Consulta se dois personagens são aliados para regras de combate (golpes de área e projéteis
/// não causam dano em aliados). Dois personagens são aliados se ambos pertencem ao mesmo grupo.
/// </summary>
public sealed class AliadosDeCombateDoSocial(IConsultaDeGrupo consulta) : IAliadosDeCombate
{
    private readonly IConsultaDeGrupo _consulta =
        consulta ?? throw new ArgumentNullException(nameof(consulta));

    public bool SaoAliados(long personagemA, long personagemB)
    {
        if (personagemA <= 0 || personagemB <= 0 || personagemA == personagemB)
        {
            return false;
        }

        var grupoA = _consulta.Consultar(personagemA);
        if (grupoA == null)
        {
            return false;
        }

        var grupoB = _consulta.Consultar(personagemB);
        return grupoB != null && grupoA.GrupoId == grupoB.GrupoId;
    }
}
