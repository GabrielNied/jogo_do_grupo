namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Client pede para coletar (tecla F). O servidor confere o dono, a distância
    /// (<see cref="RegrasLoot.DentroDoRaioDeColeta"/>, com a posição que ele tem do personagem) e
    /// o espaço no inventário. Responde com um <see cref="LootColetado"/> por item que entrou e
    /// um <see cref="ColetaRecusada"/> por pedido que não deu em nada.
    /// Id: <see cref="ItensMessageIds.ColetarLoot"/>.
    /// </summary>
    [System.Serializable]
    public class ColetarLoot
    {
        /// <summary>
        /// O item a coletar, ou <see cref="RegrasLoot.TudoPorPerto"/> para coletar tudo o que é
        /// do personagem dentro do raio de coleta.
        /// </summary>
        public long LootId = RegrasLoot.TudoPorPerto;

        /// <summary>
        /// Só vale em "tudo por perto": peças de tier abaixo disto ficam no chão (filtro de loot
        /// do jogador, #565). <see cref="TierItem.D"/> (o padrão) coleta tudo. Itens que não são
        /// equipamento não têm tier e sempre passam. Pedir um item pelo <see cref="LootId"/> ignora
        /// o filtro: é uma escolha explícita.
        /// </summary>
        public TierItem TierMinimo = TierItem.D;
    }
}
