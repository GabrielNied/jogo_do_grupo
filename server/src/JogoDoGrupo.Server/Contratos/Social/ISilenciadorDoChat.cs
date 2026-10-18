namespace JogoDoGrupo.Server.Contratos.Social;

/// <summary>
/// Permite que moderação ou regras do sistema silenciem temporariamente um personagem no chat.
/// </summary>
public interface ISilenciadorDoChat
{
    void Silenciar(long personagemId, TimeSpan duracao);
}
