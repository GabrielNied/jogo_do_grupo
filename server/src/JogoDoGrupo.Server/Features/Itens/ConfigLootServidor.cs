namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Ajustes do loot no chão que só o servidor usa. As regras do jogo (chances, prazo, raio de
/// coleta) ficam em <c>RegrasLoot</c>, no shared.
/// </summary>
public static class ConfigLootServidor
{
    /// <summary>
    /// De quanto em quanto tempo, em segundos, o servidor procura itens expirados. O item some
    /// com até esse atraso depois do prazo; a coleta confere o prazo na hora e não depende disso.
    /// </summary>
    public const double IntervaloDaVarreduraSegundos = 1.0;
}
