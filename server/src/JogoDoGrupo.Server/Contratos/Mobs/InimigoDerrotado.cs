using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Publicado por Mobs quando um inimigo morre, uma vez por morte, para quem dá a recompensa
/// (loot e experiência). <see cref="Experiencia"/> e <see cref="MultiplicadorDeRecompensa"/> já
/// contam a variante e o <see cref="Nivel"/> do inimigo (0: quem publicou não informou o
/// nível). A posição é no plano do chão, onde ele caiu.
/// <para>
/// <see cref="Participantes"/> são os personagens que causaram dano ao inimigo desde que ele
/// apareceu (a lista zera quando ele desiste e volta), em ordem crescente de id. Quem só foi
/// alvo dele, sem causar dano, não participa.
/// </para>
/// <para>
/// Sai do laço de IA de Mobs, não do laço de rede: o assinante precisa ser seguro entre threads
/// e não pode bloquear (trabalho com banco vai para uma <c>Task</c>).
/// <see cref="Sequencia"/> distingue esta morte das outras do mesmo <see cref="InimigoId"/>
/// (o id volta a ser usado quando o inimigo reaparece).
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
    IReadOnlyList<long> Participantes,
    long Sequencia,
    int Nivel = 0
);
