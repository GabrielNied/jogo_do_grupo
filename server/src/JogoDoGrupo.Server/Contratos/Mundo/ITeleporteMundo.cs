namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Permite mover um personagem para coordenadas específicas no mundo (usado por moderação e comandos de GM).
/// </summary>
public interface ITeleporteMundo
{
    (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z);
}
