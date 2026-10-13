namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Um item no chão, reservado a um personagem. O loot é individual: só o dono recebe as
    /// mensagens dele, os outros jogadores nem veem o item. Vai dentro de
    /// <see cref="LootApareceu"/>.
    /// </summary>
    [System.Serializable]
    public class LootNoChao
    {
        /// <summary>
        /// Id do item no chão, gerado pelo servidor; nunca é <see cref="RegrasLoot.TudoPorPerto"/>.
        /// </summary>
        public long LootId = 0;

        /// <summary>Id do item em <see cref="CatalogoItens"/>.</summary>
        public int ItemId = 0;

        /// <summary>Nome exibido, o mesmo de <see cref="CatalogoItens.ObterNome"/>.</summary>
        public string Nome = "";

        /// <summary>Grau da peça.</summary>
        public GrauEquipamento Grau = GrauEquipamento.Inicial;

        /// <summary>
        /// Posição no plano do chão, onde o inimigo caiu. O servidor não conhece a altura do
        /// terreno: o client assenta o item no chão.
        /// </summary>
        public float X = 0f;
        public float Z = 0f;

        /// <summary>
        /// Segundos que faltam, no momento do envio, para o item expirar (arredondado para cima;
        /// começa em <see cref="RegrasLoot.ReservaSegundos"/>).
        /// </summary>
        public int ExpiraEmSegundos = 0;

        /// <summary>
        /// Se o item é equipamento e, portanto, tem <see cref="Tier"/>. O servidor sorteia os
        /// valores da peça quando ela cai (e não mais ao ser coletada), então já sabe o tier.
        /// </summary>
        public bool TemTier = false;

        /// <summary>Tier da peça no chão (só vale com <see cref="TemTier"/>); base do filtro de loot (#565).</summary>
        public TierItem Tier = TierItem.D;
    }
}
