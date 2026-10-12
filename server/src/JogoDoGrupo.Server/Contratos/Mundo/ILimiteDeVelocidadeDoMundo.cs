namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Pedido que outra funcionalidade faz ao Mundo para limitar a velocidade de um personagem por
/// um instante (ex.: o golpe de Combate, que prende a corrida a 40% no preparo). Quem pede já
/// decidiu o limite; o Mundo aplica na validação do movimento (ADR 0016). Pode ser chamado de
/// qualquer thread. Enquanto o Mundo não implementar, quem pede trata a ausência como "sem
/// limite no servidor" e o client segura o próprio corpo.
/// </summary>
public interface ILimiteDeVelocidadeDoMundo
{
    /// <summary>
    /// Limita a velocidade do personagem a <paramref name="fator"/> (entre 0 e 1) da que ele
    /// teria por <paramref name="duracaoSegundos"/> a partir de agora. Um pedido novo substitui
    /// o anterior. Falso se o personagem não está em nenhuma área ou se os números não servem.
    /// </summary>
    bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos);
}
