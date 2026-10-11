namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Publicado por Mobs quando o golpe de um inimigo pega um personagem: ele estava na área do
/// golpe quando o aviso acabou. <see cref="Dano"/> é o dano do inimigo, já com a variante,
/// antes de qualquer defesa. Mobs não mexe na vida do personagem: quem é dono dela assina este
/// evento e decide o que o golpe tira. Um evento por personagem atingido.
/// <para>
/// Sai do laço de IA de Mobs, não do laço de rede: o assinante precisa ser seguro entre threads
/// e não pode bloquear (trabalho com banco vai para uma <c>Task</c>).
/// </para>
/// </summary>
public sealed record GolpeDeInimigoAcertou(
    long InimigoId,
    int CriaturaId,
    long PersonagemId,
    int Dano,
    string Area
);
