namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Um espaço ocupado do inventário. Itens repetidos aparecem em entradas separadas
    /// (não há pilha). O servidor envia dentro de <see cref="InventarioResponse"/> e de
    /// <see cref="AbrirBauResponse"/>.
    /// </summary>
    [System.Serializable]
    public class EntradaInventario
    {
        public long InstanciaId;
        public PosicaoEquipamento Posicao;
        public Equipamento Equipamento;

        /// <summary>Id do item em <see cref="CatalogoItens"/>.</summary>
        public int ItemId;

        /// <summary>Nome exibido, o mesmo de <see cref="CatalogoItens.ObterNome"/>.</summary>
        public string Nome = "";
    }
}
