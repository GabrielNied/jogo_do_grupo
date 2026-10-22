namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>Onde a concessão de um item de recompensa deixou o item.</summary>
public enum DestinoDaConcessao
{
    /// <summary>Não coube na bolsa: nada mudou.</summary>
    Recusado,

    /// <summary>Foi para a bolsa.</summary>
    Bolsa,

    /// <summary>Foi vestido no conjunto 1 (arma de personagem sem arma nenhuma).</summary>
    Equipado,
}

/// <summary>
/// Entrega de item de recompensa (Missões), oferecida por Itens (dono do inventário e do
/// equipamento, ADR 0016). Arma entregue a quem não tem arma nos dois conjuntos já sai vestida no
/// conjunto 1; com arma vestida, ou item que não é arma, vai para a bolsa. Participa da transação
/// do chamador e não confirma nada: depois do commit, o chamador chama
/// <see cref="AvisarAsync"/> quando o destino foi <see cref="DestinoDaConcessao.Equipado"/>.
/// </summary>
public interface IConcessaoDeItens
{
    Task<DestinoDaConcessao> ConcederAsync(long personagemId, int itemId, CancellationToken c);

    /// <summary>
    /// Avisa a área do visual e publica <see cref="EquipamentoMudou"/> (Combate invalida os
    /// atributos e Skills reenvia a barra). Só depois de gravado.
    /// </summary>
    Task AvisarAsync(long personagemId, CancellationToken c);
}
