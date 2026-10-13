using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>O teto de velocidade pedido por outra funcionalidade, guardado no estado da cidade.</summary>
internal sealed class LimiteDeVelocidadeDaCidade(CidadeDoMundo<SessaoTempoReal> cidade)
    : ILimiteDeVelocidadeDoMundo
{
    public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos) =>
        cidade.LimitarVelocidade(personagemId, fator, duracaoSegundos);
}
