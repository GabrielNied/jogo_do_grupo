using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>IDs de mensagem de Mundo. Use somente valores entre 1000 e 1999.</summary>
    public static class MundoMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(1000, 1999);

        /// <summary>Client → servidor: solicita entrada na área da cidade.</summary>
        public const ushort EntrarNaCidade = 1000;

        /// <summary>Servidor → client: estado atual da cidade ao entrar (jogadores presentes).</summary>
        public const ushort EstadoDaCidade = 1001;

        /// <summary>Client → servidor: intenção de movimento (posição desejada + sequência).</summary>
        public const ushort IntencaoDeMovimento = 1002;

        /// <summary>Servidor → client: posição autoritativa do próprio jogador ou réplica de outro.</summary>
        public const ushort PosicaoJogador = 1003;

        /// <summary>Servidor → client: outro jogador entrou na área visível.</summary>
        public const ushort JogadorEntrou = 1004;

        /// <summary>Servidor → client: outro jogador saiu da área visível.</summary>
        public const ushort JogadorSaiu = 1005;
    }
}
