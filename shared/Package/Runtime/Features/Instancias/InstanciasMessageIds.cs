using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>IDs de mensagem de Dungeons e raids. Use somente valores entre 12000 e 12999.</summary>
    public static class InstanciasMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(12000, 12999);

        /// <summary>Client → servidor: o líder inicia ou um membro entra numa corrida ativa.</summary>
        public const ushort EntrarNaDungeon = 12000;

        /// <summary>Servidor → participante: confirma a entrada numa corrida.</summary>
        public const ushort DungeonIniciada = 12001;

        /// <summary>Servidor → solicitante: a entrada foi recusada.</summary>
        public const ushort EntradaRecusada = 12002;

        /// <summary>Servidor → participantes: a corrida foi encerrada.</summary>
        public const ushort DungeonEncerrada = 12003;

        /// <summary>Servidor → participantes: o chefe da dungeon foi derrotado; carência para coleta de loot.</summary>
        public const ushort ChefeDerrotadoNaDungeon = 12004;

        /// <summary>Client → servidor: participante solicita saída da dungeon antecipadamente após a vitória.</summary>
        public const ushort SairDaDungeon = 12005;
    }
}
