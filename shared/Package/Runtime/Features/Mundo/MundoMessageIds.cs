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

        /// <summary>Servidor → client: velocidades do personagem (<see cref="Mundo.VelocidadesDeMovimento"/>).</summary>
        public const ushort VelocidadesDeMovimento = 1006;

        /// <summary>Client → servidor: confirma as velocidades aplicadas (<see cref="Mundo.ConfirmarVelocidade"/>).</summary>
        public const ushort ConfirmarVelocidade = 1007;

        /// <summary>Client → servidor: pede velocidades de teste, só em desenvolvimento (<see cref="Mundo.PedirVelocidadeDeTeste"/>).</summary>
        public const ushort PedirVelocidadeDeTeste = 1008;
    }
}
