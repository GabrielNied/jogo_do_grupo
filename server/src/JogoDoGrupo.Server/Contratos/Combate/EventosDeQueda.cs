namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Publicado por Combate quando a vida do personagem chega a 0 e ele passa a estar caído.
/// Sai de uma thread de serviço de Combate: o assinante não pode bloquear.
/// </summary>
public sealed record PersonagemCaiu(long PersonagemId, string Area);

/// <summary>
/// Publicado por Combate quando um aliado terminou de levantar o personagem: ele voltou com 20%
/// da vida máxima, no mesmo lugar. <see cref="AjudanteId"/> é quem levantou.
/// </summary>
public sealed record PersonagemLevantou(long PersonagemId, long AjudanteId, string Area);

/// <summary>
/// Publicado por Combate quando o caído sai voltando à cidade (fim dos 45 s ou pedido dele). A
/// vida, a stamina e a mana já foram enchidos por Combate. <b>Quem é dono da posição (Mundo)
/// assina este evento e leva o personagem ao ponto de nascimento da cidade</b>; Combate não
/// mexe na posição. <see cref="Area"/> é onde ele estava ao voltar.
/// </summary>
public sealed record PersonagemVoltouParaACidade(long PersonagemId, string Area);
