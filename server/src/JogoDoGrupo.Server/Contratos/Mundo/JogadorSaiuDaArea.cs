namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Publicado pelo Mundo quando um personagem deixa uma área, por sair do jogo ou por cair a
/// conexão. Quando chega aos assinantes, a consulta de posição já não encontra o personagem.
/// </summary>
public sealed record JogadorSaiuDaArea(long PersonagemId, string Area);
