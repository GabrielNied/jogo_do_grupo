using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Regras puras da compensação de latência no acerto e na esquiva (issue #172, ADR 0039).
    /// </summary>
    public static class RegrasCompensacaoDeLag
    {
        /// <summary>Histórico de posição que o servidor guarda para rebobinar.</summary>
        public const int JanelaMs = 250;

        /// <summary>
        /// Instantâneo do relógio do servidor em que o volume ou a esquiva devem ser testados.
        /// <paramref name="tempoVistoMs"/> é o relógio do servidor que o client acreditava ter ao
        /// pedir; 0 ou negativo significa “sem referência” e usa <paramref name="agoraMs"/>.
        /// </summary>
        public static long InstanteParaTeste(long tempoVistoMs, long agoraMs)
        {
            if (tempoVistoMs <= 0)
            {
                return agoraMs;
            }

            if (tempoVistoMs > agoraMs)
            {
                return agoraMs;
            }

            if (agoraMs - tempoVistoMs > JanelaMs)
            {
                return agoraMs;
            }

            return tempoVistoMs;
        }

        /// <summary>Idade máxima de uma amostra no histórico (mesma janela do rewind).</summary>
        public static long LimiteDeRetencaoMs(long agoraMs) => agoraMs - JanelaMs;
    }
}
