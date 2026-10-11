using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Tempos do estado caído. Os padrões vêm de <see cref="RegrasQueda"/> (fonte única do
/// balanceamento); os testes de integração registram uma cópia mais curta para não esperar 45 s.
/// </summary>
public sealed record ConfigQueda
{
    /// <summary>Tempo caído até voltar sozinho à cidade.</summary>
    public int TempoCaidoMs { get; init; } = RegrasQueda.DuracaoCaidoMs;

    /// <summary>Tempo que o ajudante segura a interação.</summary>
    public int TempoParaLevantarMs { get; init; } = RegrasQueda.TempoParaLevantarMs;

    /// <summary>Tempo caído antes de poder pedir para voltar à cidade.</summary>
    public int MinimoParaVoltarMs { get; init; } = RegrasQueda.MinimoParaVoltarMs;

    /// <summary>De quanto em quanto tempo o servidor confere o alcance do ajudante.</summary>
    public int IntervaloDeConferenciaMs { get; init; } = 100;
}
