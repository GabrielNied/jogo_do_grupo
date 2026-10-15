namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Quem está perto de quem, oferecido pelo Mundo às outras funcionalidades (ADR 0016; Mobs e
/// Combate usam para não varrer a área inteira). Peça pela injeção de dependência. Na cidade a
/// resposta vem da grade de interesse; em instâncias, que são pequenas, vale a área toda. Pode
/// ser chamada de qualquer thread.
/// </summary>
public interface IVizinhanca
{
    /// <summary>
    /// Os personagens que enxergam <paramref name="personagemId"/> (e que ele enxerga: a relação
    /// é simétrica). Vazio se ele não está em nenhuma área.
    /// </summary>
    IReadOnlyCollection<long> QuemVe(long personagemId);

    /// <summary>
    /// Os personagens da área a até <paramref name="raioMetros"/> do ponto, no plano horizontal.
    /// </summary>
    IReadOnlyCollection<long> PersonagensPerto(string area, float x, float z, float raioMetros);
}
