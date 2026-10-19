using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Conquistas
{
    /// <summary>IDs de mensagem de Conquistas. Faixa 18000–18999.</summary>
    public static class ConquistasMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(18000, 18999);

        public const ushort ConsultarConquistas = 18001;
        public const ushort DadosDeConquistas = 18002;
        public const ushort ConquistaDesbloqueada = 18003;
        public const ushort EquiparTitulo = 18004;
        public const ushort TituloEquipado = 18005;
        public const ushort OperacaoDeConquistaRecusada = 18006;
    }
}
