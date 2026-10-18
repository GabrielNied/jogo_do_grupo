using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Core.TempoReal
{
    /// <summary>
    /// IDs de mensagem de tempo real (UDP). Todos dentro de <see cref="CoreMessageIds.Faixa"/> (0–999).
    /// </summary>
    public static class TempoRealIds
    {
        /// <summary>Client → servidor: autentica a conexão UDP com o JWT do login.</summary>
        public const ushort AutenticarTempoReal = 1;

        /// <summary>Servidor → client: resultado da autenticação UDP.</summary>
        public const ushort ResultadoAutenticacaoTempoReal = 2;

        /// <summary>Client → servidor: comando administrativo de GM.</summary>
        public const ushort PedirComandoGm = 10;

        /// <summary>Servidor → client: resposta de comando administrativo de GM.</summary>
        public const ushort ResultadoComandoGm = 11;

        /// <summary>Client → servidor: denúncia de jogador.</summary>
        public const ushort DenunciarJogador = 12;

        /// <summary>Servidor → client: resposta do envio de denúncia.</summary>
        public const ushort ResultadoDenuncia = 13;
    }
}
