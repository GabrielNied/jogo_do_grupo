using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>IDs de mensagem de Combate. Use somente valores entre 6000 e 6999.</summary>
    public static class CombateMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(6000, 6999);

        /// <summary>Client → servidor: pedido de ataque leve ou pesado, com a direção da mira.</summary>
        public const ushort IntencaoDeAtaque = 6000;

        /// <summary>Servidor → quem está na área: um ataque começou o preparo.</summary>
        public const ushort AtaqueIniciado = 6001;

        /// <summary>Servidor → quem está na área: o golpe acertou um ou mais alvos.</summary>
        public const ushort AtaqueAcertou = 6002;

        /// <summary>Servidor → atacante: o pedido de ataque foi recusado.</summary>
        public const ushort AtaqueRecusado = 6003;

        /// <summary>Servidor → dono: vida, stamina ou mana do próprio personagem mudaram.</summary>
        public const ushort RecursosMudaram = 6004;

        /// <summary>Client → servidor: pedido de esquiva, com a direção do deslocamento.</summary>
        public const ushort IntencaoDeEsquiva = 6010;

        /// <summary>Servidor → dono: o pedido de esquiva foi recusado.</summary>
        public const ushort EsquivaRecusada = 6011;

        /// <summary>Servidor → quem está na área: um personagem começou a esquivar.</summary>
        public const ushort EsquivaIniciada = 6012;

        /// <summary>Client → servidor: o ajudante começa a levantar um aliado caído.</summary>
        public const ushort ComecarALevantar = 6020;

        /// <summary>Client → servidor: o ajudante soltou a interação.</summary>
        public const ushort CancelarLevantar = 6021;

        /// <summary>Client → servidor: o caído pede para voltar à cidade.</summary>
        public const ushort VoltarParaACidade = 6022;

        /// <summary>Servidor → caído, ajudante e quem está na área: o estado de queda mudou.</summary>
        public const ushort EstadoDeQueda = 6023;

        /// <summary>Servidor → quem pediu: o pedido do estado caído foi recusado.</summary>
        public const ushort AcaoRecusada = 6024;

        /// <summary>Client → servidor: a guarda foi segurada ou solta.</summary>
        public const ushort IntencaoDeGuarda = 6030;

        /// <summary>Servidor → quem está na área: a guarda de um personagem subiu, desceu ou quebrou.</summary>
        public const ushort GuardaMudou = 6031;

        /// <summary>Servidor → quem está na área: a guarda segurou um golpe.</summary>
        public const ushort GolpeBloqueado = 6032;

        /// <summary>Servidor → quem está na área: um acerto interrompeu uma ação.</summary>
        public const ushort AcaoInterrompida = 6033;

        /// <summary>Servidor → dono: a defesa foi insuficiente para anular o impacto do golpe.</summary>
        public const ushort DefesaInsuficiente = 6034;

        /// <summary>Servidor → dono e área: os efeitos de status de uma entidade mudaram.</summary>
        public const ushort EfeitosDeStatusMudaram = 6040;

        /// <summary>Servidor → quem está na área: a barra de quebra de um chefe mudou.</summary>
        public const ushort BarraDeQuebraMudou = 6041;

        /// <summary>Client → servidor: pedido de troca de arma (tecla R).</summary>
        public const ushort IntencaoDeTrocaDeArma = 6050;

        /// <summary>Servidor → dono: a troca de arma valeu e o bloqueio de 0,4 s começou.</summary>
        public const ushort TrocaDeArmaIniciada = 6051;

        /// <summary>Servidor → dono: o pedido de troca de arma foi recusado.</summary>
        public const ushort TrocaDeArmaRecusada = 6052;
    }
}
