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
    }
}
