namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>Um item do chão que o dono pode coletar, e onde ele está (plano do chão).</summary>
public readonly record struct LootAlcancavel(long LootId, float X, float Z);

/// <summary>Como terminou um pedido de coleta feito em nome do dono.</summary>
public enum ResultadoDaColetaParaODono
{
    /// <summary>O item entrou no inventário do dono.</summary>
    Coletado,

    /// <summary>O item já não está livre (o dono ou outro pedido o pegou, ou expirou).</summary>
    Indisponivel,

    /// <summary>Não coube no inventário (ou a gravação falhou): o item continua no chão.</summary>
    SemEspaco,
}

/// <summary>
/// O loot do chão de um personagem, oferecido por Itens a quem coleta em nome dele (Pets; ADR
/// 0016). Só enxerga o que o personagem poderia coletar: item de outro dono não aparece. A coleta
/// usa a mesma regra da tecla F (um só vencedor por item, espaço no inventário conferido na
/// gravação), e o dono recebe o <c>LootColetado</c> de sempre. Pode ser chamada de qualquer thread.
/// </summary>
public interface ILootDoDono
{
    /// <summary>
    /// O item livre do dono, na área, mais perto do ponto e a até <paramref name="raioMetros"/>
    /// dele. Falso se não há nenhum.
    /// </summary>
    bool TentarEscolher(
        long donoId,
        string area,
        float x,
        float z,
        float raioMetros,
        out LootAlcancavel loot
    );

    /// <summary>Se o item ainda está livre no chão para o dono (existe, no prazo, sem coleta em andamento).</summary>
    bool Continua(long donoId, long lootId);

    /// <summary>
    /// Coleta o item para o inventário do dono, sem checar a distância do dono (quem pede garante
    /// que está junto do item). Não manda recusa ao dono: o resultado volta a quem pediu.
    /// </summary>
    Task<ResultadoDaColetaParaODono> ColetarParaODono(long donoId, long lootId);
}
