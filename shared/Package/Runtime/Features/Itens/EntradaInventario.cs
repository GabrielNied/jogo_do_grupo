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

        /// <summary>Contêiner onde o item está guardado (a bolsa, por enquanto).</summary>
        public ConteinerItem Conteiner;

        /// <summary>Aba do contêiner (0..abas-1), ou <see cref="RegrasItens.SemCasa"/> equipado.</summary>
        public int Aba = RegrasItens.SemCasa;

        /// <summary>
        /// Casa da aba (0..casas-1), ou <see cref="RegrasItens.SemCasa"/> quando o item está
        /// equipado. O servidor grava e devolve; o client só mostra.
        /// </summary>
        public int Casa = RegrasItens.SemCasa;

        public Equipamento? Equipamento;

        /// <summary>Id do item em <see cref="CatalogoItens"/>.</summary>
        public int ItemId;

        /// <summary>Nome exibido, o mesmo de <see cref="CatalogoItens.ObterNome"/>.</summary>
        public string Nome = "";
    }
}
