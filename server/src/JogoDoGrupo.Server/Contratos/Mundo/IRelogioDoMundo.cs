namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Consulta do relógio autoritativo do mundo e do ciclo dia/noite para outras funcionalidades (ADR 0016).
/// </summary>
public interface IRelogioDoMundo
{
    /// <summary>Hora atual do mundo, de 0 até menos de 24.</summary>
    float Hora();

    /// <summary>Se é noite no momento atual.</summary>
    bool EhNoite();
}
