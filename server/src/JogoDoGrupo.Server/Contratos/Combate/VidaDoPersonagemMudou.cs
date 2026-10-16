namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Publicado por Combate sempre que a vida de um personagem muda (dano, cura, ressurreição).
/// Social assina para atualizar a vida dos membros do grupo.
/// </summary>
public sealed record VidaDoPersonagemMudou(long PersonagemId, int VidaAtual, int VidaMaxima);
