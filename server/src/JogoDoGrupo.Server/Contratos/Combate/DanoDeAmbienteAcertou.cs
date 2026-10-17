namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Publicado quando um dano ambiental atinge um personagem (ex.: lava do Mundo).
/// </summary>
public sealed record DanoDeAmbienteAcertou(long PersonagemId, int Dano, string Origem, string Area);
