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

        /// <summary>Servidor → membros: a vida atual e máxima de um membro mudou.</summary>
        public const ushort VidaDoMembroMudou = 13014;

        /// <summary>Servidor → membros: a área atual de um membro mudou (#351).</summary>
        public const ushort AreaDoMembroMudou = 13015;

        // ── Chat (13100 a 13199) ──────────────────────────────────────────────────

        /// <summary>Client → servidor: envia uma mensagem de chat em um canal.</summary>
        public const ushort EnviarMensagemDeChat = 13100;

        /// <summary>Servidor → quem recebe (e o remetente): uma mensagem de chat aceita.</summary>
        public const ushort MensagemDeChat = 13101;

        /// <summary>Servidor → remetente: a mensagem foi recusada e não chegou a ninguém.</summary>
        public const ushort MensagemDeChatRecusada = 13102;

        /// <summary>Servidor → quem entra na cidade: as últimas mensagens do canal Cidade.</summary>
        public const ushort HistoricoDaCidade = 13103;

        // ── Amigos e ignorados: pedidos do client (13200 a 13209) ─────────────────

        /// <summary>Client → servidor: pede amizade ao dono de um personagem, pelo nome.</summary>
        public const ushort PedirAmizade = 13200;

        /// <summary>Client → servidor: aceita ou recusa um pedido de amizade recebido.</summary>
        public const ushort ResponderPedidoDeAmizade = 13201;

        /// <summary>Client → servidor: desiste de um pedido de amizade enviado.</summary>
        public const ushort CancelarPedidoDeAmizade = 13202;

        /// <summary>Client → servidor: desfaz uma amizade.</summary>
        public const ushort RemoverAmigo = 13203;

        /// <summary>Client → servidor: passa a ignorar o dono de um personagem, pelo nome.</summary>
        public const ushort IgnorarJogador = 13204;

        /// <summary>Client → servidor: tira um jogador da lista de ignorados.</summary>
        public const ushort DeixarDeIgnorar = 13205;

        // ── Amigos e ignorados: respostas do servidor (13210 a 13219) ─────────────

        /// <summary>Servidor → client: a lista completa de amigos e de pedidos pendentes.</summary>
        public const ushort ListaDeAmigos = 13210;

        /// <summary>Servidor → client: um amigo entrou, saiu ou trocou de personagem.</summary>
        public const ushort AmigoMudouDeEstado = 13211;

        /// <summary>Servidor → client: a lista completa de ignorados.</summary>
        public const ushort ListaDeIgnorados = 13212;

        /// <summary>Servidor → quem pediu: a operação de amigos ou ignorados foi recusada.</summary>
        public const ushort OperacaoDeAmizadeRecusada = 13213;

        // ── Marcadores táticos de alvo do grupo (13320 a 13329) ───────────────────

        /// <summary>Client → servidor: define ou remove um marcador tático em um alvo.</summary>
        public const ushort DefinirMarcadorDeAlvo = 13320;

        /// <summary>Servidor → membros: a lista de alvos marcados taticamente pelo grupo mudou.</summary>
        public const ushort MarcadoresDeAlvoMudaram = 13321;

        /// <summary>Servidor → solicitante: o pedido de definir marcador foi recusado.</summary>
        public const ushort DefinirMarcadorRecusado = 13322;

        // ── Inspeção de jogador (13300 a 13319) ───────────────────────────────────

        /// <summary>Client → servidor: pede para inspecionar um personagem alvo.</summary>
        public const ushort PedirInspecao = 13300;

        /// <summary>Servidor → client: retrato completo do equipamento e atributos do alvo inspecionado.</summary>
        public const ushort RetratoInspecaoResponse = 13310;

        /// <summary>Servidor → quem pediu: o pedido de inspeção foi recusado.</summary>
        public const ushort InspecaoRecusada = 13311;

        // ── Fila de grupo para dungeon (13330 a 13339) ───────────────────────────

        /// <summary>Client → servidor: pede para entrar na fila de dungeon com um papel.</summary>
        public const ushort EntrarNaFilaDeGrupo = 13330;

        /// <summary>Client → servidor: cancela a inscrição na fila de dungeon.</summary>
        public const ushort SairDaFilaDeGrupo = 13331;

        /// <summary>Servidor → client: status da fila de dungeon para o personagem.</summary>
        public const ushort StatusDaFilaDeGrupo = 13332;

        /// <summary>Servidor → membros: o grupo da fila foi formado com sucesso.</summary>
        public const ushort GrupoDeFilaFormado = 13333;
    }
}
