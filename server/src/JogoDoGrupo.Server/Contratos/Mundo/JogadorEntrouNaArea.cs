namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Publicado pelo Mundo quando um personagem passa a estar numa área (uma vez por entrada;
/// pedir para entrar de novo já estando nela não repete o evento).
/// </summary>
public sealed record JogadorEntrouNaArea(long PersonagemId, string Area);
