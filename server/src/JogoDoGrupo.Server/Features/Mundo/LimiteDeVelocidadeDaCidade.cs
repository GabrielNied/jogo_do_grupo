using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>O teto de velocidade pedido por outra funcionalidade, guardado no estado da cidade.</summary>
internal sealed class LimiteDeVelocidadeDaCidade(EstadoDasAreasDoMundo<SessaoTempoReal> areas)
    : ILimiteDeVelocidadeDoMundo
{
    public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos) =>
        areas.ParaPersonagem(personagemId)?.LimitarVelocidade(personagemId, fator, duracaoSegundos)
        ?? false;
}
