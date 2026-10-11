using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>IDs de mensagem de Social. Use somente valores entre 13000 e 13999.</summary>
    public static class SocialMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(13000, 13999);

        // ── Grupo: pedidos do client (13000 a 13009) ──────────────────────────────

        /// <summary>Client → servidor: convida um personagem, pelo nome, para o grupo.</summary>
        public const ushort ConvidarParaGrupo = 13000;

        /// <summary>Client → servidor: aceita ou recusa um convite recebido.</summary>
        public const ushort ResponderConvite = 13001;

        /// <summary>Client → servidor: sai do grupo em que está.</summary>
        public const ushort SairDoGrupo = 13002;

        /// <summary>Client → servidor: o líder expulsa um membro.</summary>
        public const ushort ExpulsarDoGrupo = 13003;

        /// <summary>Client → servidor: o líder passa a liderança a outro membro.</summary>
        public const ushort PassarLideranca = 13004;

        // ── Grupo: respostas do servidor (13010 a 13019) ──────────────────────────

        /// <summary>Servidor → convidado: chegou um convite de grupo.</summary>
        public const ushort ConviteRecebido = 13010;

        /// <summary>Servidor → membros: a lista completa e atual do grupo (vazia = sem grupo).</summary>
        public const ushort ListaDoGrupo = 13011;

        /// <summary>Servidor → client: algo aconteceu no grupo ou com um convite (não é erro).</summary>
        public const ushort AvisoDoGrupo = 13012;

        /// <summary>Servidor → quem pediu: a operação de grupo foi recusada.</summary>
        public const ushort OperacaoDoGrupoRecusada = 13013;

        // ── Chat (13100 a 13199) ──────────────────────────────────────────────────

        /// <summary>Client → servidor: envia uma mensagem de chat em um canal.</summary>
        public const ushort EnviarMensagemDeChat = 13100;

        /// <summary>Servidor → quem recebe (e o remetente): uma mensagem de chat aceita.</summary>
        public const ushort MensagemDeChat = 13101;

        /// <summary>Servidor → remetente: a mensagem foi recusada e não chegou a ninguém.</summary>
        public const ushort MensagemDeChatRecusada = 13102;

        /// <summary>Servidor → quem entra na cidade: as últimas mensagens do canal Cidade.</summary>
        public const ushort HistoricoDaCidade = 13103;
    }
}
