namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Tier de um equipamento, calculado só pelos valores dos atributos (TIER-001). A ordem é a
    /// da qualidade: <c>D &lt; C &lt; B &lt; A &lt; S &lt; S+</c>. <see cref="SMais"/> não é sorteado:
    /// é a condição de um item S com todos os atributos elegíveis no máximo (TIER-005).
    /// </summary>
    public enum TierItem
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SMais = 5,
    }
}
