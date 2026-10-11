using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Publicado por Mobs quando um inimigo morre, uma vez por morte, para quem dá a recompensa
/// (loot e experiência). <see cref="Experiencia"/> e <see cref="MultiplicadorDeRecompensa"/> já
/// contam a variante. A posição é no plano do chão, onde ele caiu.
/// <para>
/// <see cref="Participantes"/> são os personagens que o grupo do inimigo tinha à vista na hora
/// da morte (pode vir vazio). É provisório: quando o Combate informar quem deu cada golpe,
/// passa a ser quem causou dano.
/// </para>
/// <para>
/// Sai do laço de IA de Mobs, não do laço de rede: o assinante precisa ser seguro entre threads
/// e não pode bloquear (trabalho com banco vai para uma <c>Task</c>).
/// </para>
/// </summary>
public sealed record InimigoDerrotado(
    long InimigoId,
    int CriaturaId,
    VarianteDeCriatura Variante,
    int Experiencia,
    double MultiplicadorDeRecompensa,
    string Area,
    double X,
    double Z,
    IReadOnlyList<long> Participantes
);
