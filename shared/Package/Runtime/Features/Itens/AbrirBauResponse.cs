namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor responde (200) a <c>POST</c> <see cref="RotasItens.Bau"/>. Arrays nunca são
    /// <c>null</c>.
    /// </summary>
    [System.Serializable]
    public class AbrirBauResponse
    {
        /// <summary>
        /// Itens ganhos só nesta abertura, de <see cref="RegrasItens.MinimoItensPorBau"/> a
        /// <see cref="RegrasItens.MaximoItensPorBau"/> entradas.
        /// </summary>
        public EntradaInventario[] Ganhos = System.Array.Empty<EntradaInventario>();

        /// <summary>O inventário inteiro depois de gravar os ganhos, no mesmo formato do GET.</summary>
        public EntradaInventario[] Inventario = System.Array.Empty<EntradaInventario>();

        /// <summary>Capacidade da bolsa depois da abertura.</summary>
        public CapacidadeBolsa Capacidade = RegrasItens.CapacidadeInicial();
    }
}
