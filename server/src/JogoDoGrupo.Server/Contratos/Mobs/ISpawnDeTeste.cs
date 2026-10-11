namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Faz um inimigo comum aparecer perto do personagem. Só o comando de teste de Desenvolvimento chama.
/// </summary>
public interface ISpawnDeTeste
{
    (bool Aceito, string Texto) SpawnarPerto(long personagemId);
}
