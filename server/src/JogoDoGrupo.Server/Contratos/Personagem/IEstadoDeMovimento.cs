namespace JogoDoGrupo.Server.Contratos.Personagem;

/// <summary>
/// Consulta do nado e da escalada, oferecida por Personagem (dono do movimento especial, #349) às
/// outras funcionalidades (ADR 0016). Combate usa para recusar ataque a quem nada ou escala. A
/// resposta vem da posição que o Mundo aceitou, nunca do que o client diz. Pode ser chamada de
/// qualquer thread.
/// </summary>
public interface IEstadoDeMovimento
{
    /// <summary>Verdadeiro enquanto o personagem está nadando (água funda, pés abaixo da superfície).</summary>
    bool EstaNadando(long personagemId);

    /// <summary>Verdadeiro enquanto o personagem está numa via de escalada, escalando.</summary>
    bool EstaEscalando(long personagemId);
}
