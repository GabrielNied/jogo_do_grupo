namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor avisa o dono que o item saiu do chão e já está gravado no inventário dele.
    /// Id: <see cref="ItensMessageIds.LootColetado"/>.
    /// </summary>
    [System.Serializable]
    public class LootColetado
    {
        public long LootId = 0;

        /// <summary>A entrada nova do inventário (ocupa um espaço).</summary>
        public EntradaInventario Entrada = new EntradaInventario();
    }
}
