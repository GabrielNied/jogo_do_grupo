using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>IDs de mensagem de Missões. Use somente valores entre 10000 e 10999.</summary>
    public static class MissoesMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(10000, 10999);

        public const ushort ConsultarMissoes = 10000;
        public const ushort AceitarMissao = 10001;
        public const ushort AbandonarMissao = 10002;
        public const ushort EntregarMissao = 10003;
        public const ushort CompartilharMissao = 10004;
        public const ushort AlternarAcompanhamento = 10005;
        public const ushort InteragirComNpcMissao = 10006;

        public const ushort EstadoDasMissoes = 10010;
        public const ushort MissaoAtualizada = 10011;
        public const ushort OperacaoDeMissaoRecusada = 10012;
        public const ushort ConviteDeMissaoCompartilhada = 10013;
        public const ushort AvisoDeMissao = 10014;
    }
}
