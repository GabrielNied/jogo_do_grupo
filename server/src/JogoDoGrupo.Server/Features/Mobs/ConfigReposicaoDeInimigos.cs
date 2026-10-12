namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Valores ajustáveis da reposição dos inimigos comuns do campo (proposta, a calibrar; issue
/// #489). O intervalo base de cada criatura é da ficha; aqui fica só a variação sobre ele.
/// </summary>
public static class ConfigReposicaoDeInimigos
{
    /// <summary>
    /// Folga máxima sobre o intervalo da ficha, em fração (0,25 = até 25% a mais), sorteada a
    /// cada morte para os grupos e os membros não voltarem todos no mesmo instante. Só soma,
    /// nunca tira: o intervalo da ficha é o mínimo.
    /// </summary>
    public const double VariacaoDoIntervalo = 0.25;
}
