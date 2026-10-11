namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor responde (200) a <c>GET</c> <see cref="RotasItens.Inventario"/> com as entradas
    /// do inventário do personagem, no máximo <see cref="RegrasItens.EspacosInventario"/>.
    /// Inventário vazio recebe array vazio, nunca <c>null</c>.
    /// </summary>
    [System.Serializable]
    public class InventarioResponse
    {
        public EntradaInventario[] Entradas = System.Array.Empty<EntradaInventario>();
    }
}
