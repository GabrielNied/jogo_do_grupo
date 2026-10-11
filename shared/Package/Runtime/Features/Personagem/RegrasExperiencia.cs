using System;

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>Cálculos puros; conceder XP e impedir prêmios duplicados cabe ao servidor.</summary>
    public static class RegrasExperiencia
    {
        public const int NivelMaximo = ConfigExperiencia.NivelMaximo;
        public const int PontosPorNivel = ConfigExperiencia.PontosPorNivel;

        /// <summary>Valor da curva, inclusive no teto; no teto não há subida de nível.</summary>
        public static long XpParaOProximoNivel(int nivel)
        {
            ValidarNivel(nivel);
            return checked(
                (long)
                    Math.Round(
                        ConfigExperiencia.BaseXp * Math.Pow(nivel, ConfigExperiencia.ExpoenteCurva),
                        MidpointRounding.AwayFromZero
                    )
            );
        }

        /// <summary>
        /// Retorna nível, XP restante e apenas os pontos ganhos nesta aplicação.
        /// Ganhos não positivos preservam o estado. XP negativo e nível inválido são recusados.
        /// Valores que excedam long geram OverflowException, sem truncar o XP.
        /// </summary>
        public static ResultadoExperiencia AplicarGanho(int nivel, long xp, long ganho)
        {
            ValidarNivel(nivel);
            if (xp < 0)
                throw new ArgumentOutOfRangeException(nameof(xp));
            if (ganho <= 0)
                return new ResultadoExperiencia(nivel, xp, 0);

            var restante = checked(xp + ganho);
            var nivelInicial = nivel;
            while (nivel < NivelMaximo)
            {
                var necessario = XpParaOProximoNivel(nivel);
                if (restante < necessario)
                    break;
                restante -= necessario;
                nivel++;
            }

            return new ResultadoExperiencia(
                nivel,
                restante,
                checked((nivel - nivelInicial) * PontosPorNivel)
            );
        }

        private static void ValidarNivel(int nivel)
        {
            if (nivel < 1 || nivel > NivelMaximo)
                throw new ArgumentOutOfRangeException(nameof(nivel));
        }
    }

    public readonly struct ResultadoExperiencia
    {
        public int Nivel { get; }
        public long Xp { get; }
        public int PontosGanhos { get; }

        public ResultadoExperiencia(int nivel, long xp, int pontosGanhos)
        {
            Nivel = nivel;
            Xp = xp;
            PontosGanhos = pontosGanhos;
        }
    }
}
