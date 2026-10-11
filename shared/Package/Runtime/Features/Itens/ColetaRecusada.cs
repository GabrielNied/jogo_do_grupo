namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor avisa o dono que o pedido de coleta não deu em nada. O item, se existe, continua
    /// no chão até expirar.
    /// Id: <see cref="ItensMessageIds.ColetaRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class ColetaRecusada
    {
        /// <summary>O mesmo valor de <see cref="ColetarLoot.LootId"/> do pedido recusado.</summary>
        public long LootId = 0;

        /// <summary>
        /// Motivo: <see cref="CodigosErroItens.LootNaoEncontrado"/>,
        /// <see cref="CodigosErroItens.LongeDemais"/> ou
        /// <see cref="CodigosErroItens.InventarioCheio"/>.
        /// </summary>
        public string Codigo = "";
    }
}
