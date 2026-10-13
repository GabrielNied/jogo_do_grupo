using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>IDs de mensagem de Itens. Use somente valores entre 7000 e 7999.</summary>
    public static class ItensMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(7000, 7999);

        /// <summary>Servidor → o dono: um item dele apareceu no chão (ou ainda está lá).</summary>
        public const ushort LootApareceu = 7000;

        /// <summary>Client → servidor: pedido para coletar um item do chão, ou todos por perto.</summary>
        public const ushort ColetarLoot = 7001;

        /// <summary>Servidor → o dono: o item saiu do chão e entrou no inventário.</summary>
        public const ushort LootColetado = 7002;

        /// <summary>Servidor → o dono: o pedido de coleta foi recusado.</summary>
        public const ushort ColetaRecusada = 7003;

        /// <summary>Servidor → o dono: o item expirou e saiu do chão.</summary>
        public const ushort LootSumiu = 7004;

        /// <summary>
        /// Servidor → a área: o visual equipado de um personagem (as armas e onde ficam). Vale o
        /// estado inteiro; o client troca o que tinha.
        /// </summary>
        public const ushort VisualEquipado = 7005;
    }
}
