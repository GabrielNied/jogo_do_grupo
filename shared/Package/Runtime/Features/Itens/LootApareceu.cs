namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Servidor avisa o dono que um item dele está no chão: quando o inimigo morre e, de novo,
    /// quando o dono volta ao jogo com o item ainda dentro do prazo. O client troca o que já
    /// tinha com o mesmo <see cref="LootNoChao.LootId"/>.
    /// Id: <see cref="ItensMessageIds.LootApareceu"/>.
    /// </summary>
    [System.Serializable]
    public class LootApareceu
    {
        public LootNoChao Loot = new LootNoChao();
    }
}
