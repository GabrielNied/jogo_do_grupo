using JogoDoGrupo.Server.Contratos.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Regra de quem um golpe pode acertar entre os alvos que estão no volume: nunca o próprio
/// atacante nem um aliado do grupo (skills de área não causam dano entre aliados). Boneco,
/// inimigos e chefes (<c>PersonagemId</c> 0) e jogadores fora do grupo do atacante continuam
/// alvos válidos.
/// </summary>
public static class SelecaoDeAlvos
{
    public static bool PodeAcertar(long atacanteId, AlvoDeCombate alvo, IAliadosDeCombate aliados)
    {
        if (alvo.PersonagemId == 0)
        {
            return true;
        }

        return alvo.PersonagemId != atacanteId
            && !aliados.SaoAliados(atacanteId, alvo.PersonagemId);
    }
}
