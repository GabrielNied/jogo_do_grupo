namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Consulta do estado caído, oferecida por Combate (dono do estado) às outras funcionalidades
/// (ADR 0016). Mobs usa para ignorar quem está caído: não atacar nem contar como alvo. Pode ser
/// chamada de qualquer thread.
/// </summary>
public interface IEstadoDeQueda
{
    /// <summary>
    /// Verdadeiro enquanto o personagem está caído ou sendo levantado (inclusive no instante
    /// em que a saída está sendo gravada). Falso para quem está vivo ou não existe.
    /// </summary>
    bool EstaCaido(long personagemId);
}
