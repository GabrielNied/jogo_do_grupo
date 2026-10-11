namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Manda o personagem para uma posição da cidade. Só o comando de teste de Desenvolvimento chama.
/// </summary>
public interface ITeleporteDeTeste
{
    (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z);
}
