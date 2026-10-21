namespace JogoDoGrupo.Server.Contratos.Npcs;

/// <summary>
/// Publicado pelos NPCs quando o servidor aceita uma conversa: o NPC existe e o personagem está
/// na mesma área e dentro do raio de interação, conferido com a posição que o Mundo aceitou
/// (sem posição conhecida não há evento). Quem conta conversa, como as missões com objetivo
/// <c>Falar</c>, assina este evento em vez de confiar num pedido do client.
/// <para>
/// Sai do laço de rede: o assinante não pode bloquear (trabalho com banco vai para uma
/// <c>Task</c>).
/// </para>
/// </summary>
public sealed record JogadorFalouComNpc(long PersonagemId, int NpcId);
