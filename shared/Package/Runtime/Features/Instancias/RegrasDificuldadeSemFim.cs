using System;

namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Regras da progressão de dificuldade sem fim nas dungeons (estilo Mythic+).
    /// O líder escolhe o nível da corrida (até 1 acima do maior nível já vencido).
    /// Cada nível soma 8% de vida e de dano aos inimigos. Há cronômetro de 15 minutos (900s):
    /// vencer no tempo libera o próximo nível. Sem afixos/modificadores na v1.
    /// </summary>
    public static class RegrasDificuldadeSemFim
    {
        public const int NivelMinimo = 1;
        public const double AumentoPorNivel = 0.08; // 8% por nível além do 1
        public const int TempoLimitePadraoSegundos = 900; // 15 minutos

        /// <summary>
        /// Multiplicador de vida dos inimigos para o nível da corrida.
        /// Nível 1 = 1.0 (100%), Nível 2 = 1.08 (+8%), Nível 3 = 1.16 (+16%), etc.
        /// </summary>
        public static double CalcularMultiplicadorVida(int nivel)
        {
            var nivelAjustado = Math.Max(NivelMinimo, nivel);
            return 1.0 + (AumentoPorNivel * (nivelAjustado - 1));
        }

        /// <summary>
        /// Multiplicador de dano dos inimigos para o nível da corrida.
        /// Nível 1 = 1.0 (100%), Nível 2 = 1.08 (+8%), Nível 3 = 1.16 (+16%), etc.
        /// </summary>
        public static double CalcularMultiplicadorDano(int nivel)
        {
            var nivelAjustado = Math.Max(NivelMinimo, nivel);
            return 1.0 + (AumentoPorNivel * (nivelAjustado - 1));
        }

        /// <summary>
        /// Verifica se a corrida foi concluída dentro do tempo limite do cronômetro.
        /// </summary>
        public static bool VenceuNoTempo(
            int segundosDecorridos,
            int tempoLimiteSegundos = TempoLimitePadraoSegundos
        )
        {
            return segundosDecorridos >= 0 && segundosDecorridos <= tempoLimiteSegundos;
        }

        /// <summary>
        /// Valida se o nível requisitado está liberado para o líder iniciar.
        /// </summary>
        public static bool NivelPermitido(int nivelRequisitado, int maiorNivelVencido)
        {
            if (nivelRequisitado < NivelMinimo)
            {
                return false;
            }

            var tetoPermitido = Math.Max(NivelMinimo, maiorNivelVencido + 1);
            return nivelRequisitado <= tetoPermitido;
        }

        /// <summary>
        /// Calcula o novo recorde de nível vencido pelo jogador após o desfecho da corrida.
        /// </summary>
        public static int CalcularNovoMaiorNivel(
            int nivelCorrida,
            int maiorNivelVencido,
            bool noTempo
        )
        {
            if (noTempo && nivelCorrida > maiorNivelVencido)
            {
                return nivelCorrida;
            }

            return maiorNivelVencido;
        }
    }
}
