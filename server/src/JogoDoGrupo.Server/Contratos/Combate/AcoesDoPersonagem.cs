namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Publicado por Combate (dono do golpe comum), <b>depois de confirmar</b> um golpe do personagem
/// que acertou ao menos um alvo. Skills escuta para os poderes de equipamento que reagem a golpes
/// seguidos (#629). Sai do laço de rede: o assinante não pode bloquear (ADR 0016).
/// </summary>
public sealed record GolpeComumAcertou(long PersonagemId, IReadOnlyList<long> AlvoIds);

/// <summary>
/// Publicado por Combate quando uma esquiva do personagem começa e vale (não foi recusada).
/// Skills escuta para os poderes de equipamento que reagem à esquiva (#629). O assinante não
/// pode bloquear (ADR 0016).
/// </summary>
public sealed record PersonagemEsquivou(long PersonagemId);
