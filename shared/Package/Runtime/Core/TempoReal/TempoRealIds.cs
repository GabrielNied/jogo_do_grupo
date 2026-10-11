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
    }
}
