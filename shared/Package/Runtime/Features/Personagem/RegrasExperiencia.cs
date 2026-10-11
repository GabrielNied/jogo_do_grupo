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

        /// <summary>
        /// Soma ou tira experiência. Quantidade positiva segue <see cref="AplicarGanho"/>.
        /// Negativa desce de nível: devolve a barra do nível anterior e tira
        /// <see cref="PontosPorNivel"/> dos pontos livres por nível perdido, sem passar de zero
        /// e sem desfazer pontos já gastos. Zero preserva o estado. Só o comando de teste usa.
        /// </summary>
        public static ResultadoAjusteDeExperiencia AplicarAjuste(
            int nivel,
            long xp,
            int pontosLivres,
            long delta
        )
        {
            ValidarNivel(nivel);
            if (xp < 0)
                throw new ArgumentOutOfRangeException(nameof(xp));
            if (pontosLivres < 0)
                throw new ArgumentOutOfRangeException(nameof(pontosLivres));
            if (delta == 0)
                return new ResultadoAjusteDeExperiencia(nivel, xp, pontosLivres);

            if (delta > 0)
            {
                var ganho = AplicarGanho(nivel, xp, delta);
                return new ResultadoAjusteDeExperiencia(
                    ganho.Nivel,
                    ganho.Xp,
                    checked(pontosLivres + ganho.PontosGanhos)
                );
            }

            var restante = checked(xp + delta);
            var nivelInicial = nivel;
            while (restante < 0 && nivel > 1)
            {
                nivel--;
                restante = checked(restante + XpParaOProximoNivel(nivel));
            }

            if (restante < 0)
                restante = 0;

            var pontos = pontosLivres - checked((nivelInicial - nivel) * PontosPorNivel);
            if (pontos < 0)
                pontos = 0;

            return new ResultadoAjusteDeExperiencia(nivel, restante, pontos);
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

    /// <summary>Nível, XP dentro do nível e pontos livres depois de <see cref="RegrasExperiencia.AplicarAjuste"/>.</summary>
    public readonly struct ResultadoAjusteDeExperiencia
    {
        public int Nivel { get; }
        public long Xp { get; }
        public int PontosLivres { get; }

        public ResultadoAjusteDeExperiencia(int nivel, long xp, int pontosLivres)
        {
            Nivel = nivel;
            Xp = xp;
            PontosLivres = pontosLivres;
        }
    }
}
