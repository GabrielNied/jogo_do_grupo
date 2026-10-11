namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor avisa o dono que o item expirou sem ser coletado e saiu do chão.
    /// Id: <see cref="ItensMessageIds.LootSumiu"/>.
    /// </summary>
    [System.Serializable]
    public class LootSumiu
    {
        public long LootId = 0;
    }
}
