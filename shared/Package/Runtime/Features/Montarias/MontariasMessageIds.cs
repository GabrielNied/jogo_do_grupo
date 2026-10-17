using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>IDs de mensagem de Montarias. Use somente valores entre 16000 e 16999.</summary>
    public static class MontariasMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(16000, 16999);

        /// <summary>Client → servidor: pede para invocar/montar na montaria.</summary>
        public const ushort ChamarMontaria = 16000;

        /// <summary>Client → servidor: pede para desmontar.</summary>
        public const ushort Desmontar = 16001;

        /// <summary>Servidor → client (broadcast): avisa novo estado de montaria de um personagem.</summary>
        public const ushort EstadoMontaria = 16002;

        /// <summary>Client → servidor: equipa uma montaria da coleção.</summary>
        public const ushort EquiparMontaria = 16003;

        /// <summary>Servidor → client: confirmação da montaria equipada.</summary>
        public const ushort MontariaEquipada = 16004;

        /// <summary>Client → servidor: usa um item consumível de montaria para registrar na conta.</summary>
        public const ushort UsarItemMontaria = 16005;

        /// <summary>Servidor → client: lista de montarias desbloqueadas pela conta.</summary>
        public const ushort ColecaoMontarias = 16006;

        /// <summary>Servidor → client: notifica desbloqueio de nova montaria na conta.</summary>
        public const ushort MontariaDesbloqueada = 16007;

        /// <summary>Servidor → client: notifica recusa ao tentar usar ou equipar montaria.</summary>
        public const ushort MontariaRecusada = 16008;
    }
}
