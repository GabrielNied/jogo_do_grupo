namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Publicado pelo Mundo quando um personagem deixa uma área, por sair do jogo ou por cair a
/// conexão ou por uma transição. Quando chega aos assinantes, ele já não está na área informada;
/// em uma transição, a consulta global pode encontrá-lo na nova área.
/// </summary>
public sealed record JogadorSaiuDaArea(long PersonagemId, string Area);
