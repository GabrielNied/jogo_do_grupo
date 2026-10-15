namespace JogoDoGrupo.Server.Contratos.Montarias;

/// <summary>
/// Consulta se um personagem está montado (ADR 0016).
/// </summary>
public interface IMontariasConsulta
{
    /// <summary>Verdadeiro se o personagem está montado no momento.</summary>
    bool EstaMontado(long personagemId);

    /// <summary>Tipo da montaria se estiver montado, ou null se a pé.</summary>
    int? TipoDaMontaria(long personagemId);
}
