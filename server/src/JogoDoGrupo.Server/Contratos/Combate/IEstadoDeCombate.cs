namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Consulta se um personagem está em combate (ADR 0016), usada para impedir ações proibidas
/// durante combate (ex.: redistribuição de talentos).
/// </summary>
public interface IEstadoDeCombate
{
    /// <summary>Verdadeiro se o personagem está em combate.</summary>
    bool EmCombate(long personagemId);
}
