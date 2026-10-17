namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Permite aplicar dano direto ou de ambiente a um personagem (ex.: dano da lava do Mundo).
/// Combate é o dono da vida e decide persistência, modificadores e notificações.
/// </summary>
public interface IDanoAoPersonagem
{
    void AplicarDanoDeAmbiente(long personagemId, int dano, string origem, string area);
}
