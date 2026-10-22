using System;

namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Regras de balanceamento e validação de PvP: duelo 1v1, arena 3v3 e bandeira no mundo aberto.
    /// </summary>
    public static class RegrasPvp
    {
        public const float DistanciaMaximaConviteDueloMetros = 15f;
        public const float RaioMaximoDoDueloMetros = 50f;
        public const int TempoContagemRegressivaDueloSegundos = 3;
        public const int TempoExpiracaoConviteDueloSegundos = 30;

        public const int TamanhoEquipeArena = 3;
        public const int TotalJogadoresArena = 6;
        public const int TempoPreparoArenaSegundos = 5;

        public const int PontosIniciaisArena = 1000;
        public const int PontosVitoriaArena = 25;
        public const int PontosDerrotaArena = 15;

        /// <summary>
        /// Calcula a nova pontuação de arena após uma vitória ou derrota.
        /// A pontuação não desce abaixo de zero.
        /// </summary>
        public static int CalcularPontosAposPartida(int pontosAtuais, bool vitoria)
        {
            if (vitoria)
            {
                return pontosAtuais + PontosVitoriaArena;
            }

            return Math.Max(0, pontosAtuais - PontosDerrotaArena);
        }

        /// <summary>
        /// Verifica se um duelista ainda está dentro da arena do duelo (raio de 50m a partir do centro).
        /// </summary>
        public static bool EstaNoRaioDoDuelo(
            double centroX,
            double centroZ,
            double jogadorX,
            double jogadorZ,
            float raioMaximo = RaioMaximoDoDueloMetros
        )
        {
            var dx = jogadorX - centroX;
            var dz = jogadorZ - centroZ;
            return (dx * dx) + (dz * dz) <= (raioMaximo * raioMaximo);
        }

        /// <summary>
        /// Verifica se a distância entre desafiante e desafiado permite iniciar um duelo.
        /// </summary>
        public static bool DistanciaValidaParaConvite(
            double x1,
            double z1,
            double x2,
            double z2,
            float maximo = DistanciaMaximaConviteDueloMetros
        )
        {
            var dx = x1 - x2;
            var dz = z1 - z2;
            return (dx * dx) + (dz * dz) <= (maximo * maximo);
        }
    }
}
