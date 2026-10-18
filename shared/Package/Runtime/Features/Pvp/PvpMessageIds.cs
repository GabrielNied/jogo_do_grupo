using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>IDs de mensagem de PvP. Use somente valores entre 14000 e 14999.</summary>
    public static class PvpMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(14000, 14999);

        // Duelo 1v1 (14000–14009)
        public const ushort SolicitarDuelo = 14000;
        public const ushort ConviteDeDueloRecebido = 14001;
        public const ushort ResponderConviteDeDuelo = 14002;
        public const ushort DueloIniciado = 14003;
        public const ushort DueloRecusado = 14004;
        public const ushort DueloEncerrado = 14005;
        public const ushort CancelarDuelo = 14006;

        // Arena 3v3 (14010–14019)
        public const ushort EntrarNaFilaDeArena = 14010;
        public const ushort SairDaFilaDeArena = 14011;
        public const ushort StatusFilaDeArena = 14012;
        public const ushort PartidaDeArenaIniciada = 14013;
        public const ushort CombatenteDeArenaEliminado = 14014;
        public const ushort PartidaDeArenaEncerrada = 14015;
        public const ushort ConsultarRankingArena = 14016;
        public const ushort RespostaRankingArena = 14017;

        // Bandeira PvP no Mundo Aberto (14020–14029)
        public const ushort AlternarBandeiraPvp = 14020;
        public const ushort BandeiraPvpAlterada = 14021;
    }
}
