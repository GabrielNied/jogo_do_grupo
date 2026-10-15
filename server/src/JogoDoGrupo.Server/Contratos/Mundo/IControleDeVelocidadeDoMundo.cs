namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Permite que outras funcionalidades (como Montarias) definam velocidades autoritativas
/// de um personagem no Mundo.
/// </summary>
public interface IControleDeVelocidadeDoMundo
{
    /// <summary>
    /// Altera a velocidade autoritativa (m/s) do personagem no servidor e avisa o client
    /// via mensagem de velocidades. Falso se o personagem não está presente em nenhuma área.
    /// </summary>
    bool DefinirVelocidades(long personagemId, float andar, float correr);
}
