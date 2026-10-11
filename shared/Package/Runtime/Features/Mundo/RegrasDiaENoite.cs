using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Regras puras do ciclo de dia e noite (issue #163). A hora vai de 0 (meia-noite) até menos
    /// de 24 e anda no mesmo ritmo o dia todo. O dono do relógio é o servidor; o client usa isto
    /// para avançar a hora entre duas mensagens <see cref="HoraDoMundo"/> e para saber se é
    /// noite. Nesta leva a noite é só cenário: não esconde inimigo nem muda regra.
    /// </summary>
    public static class RegrasDiaENoite
    {
        // Valores a calibrar. A proposta do livro (40 min de dia e 20 de noite) segue em aberto
        // em docs/features/mundo.md.

        /// <summary>Horas de um dia do mundo.</summary>
        public const float HorasPorDia = 24f;

        /// <summary>
        /// Segundos reais de um dia inteiro do mundo (20 minutos). Diminuir faz o sol andar mais
        /// depressa.
        /// </summary>
        public const float DuracaoDoDiaSegundos = 1200f;

        /// <summary>Hora do mundo quando o servidor liga (manhã).</summary>
        public const float HoraInicial = 8f;

        /// <summary>Hora em que a noite começa e as lanternas acendem.</summary>
        public const float InicioDaNoite = 20f;

        /// <summary>Hora em que a noite acaba e as lanternas apagam.</summary>
        public const float FimDaNoite = 5f;

        /// <summary>
        /// Traz qualquer hora para a faixa de 0 até menos de 24 (25 vira 1; -1 vira 23). Valor
        /// não finito vira <see cref="HoraInicial"/>.
        /// </summary>
        public static float Normalizar(double hora)
        {
            if (double.IsNaN(hora) || double.IsInfinity(hora))
            {
                return HoraInicial;
            }

            var resto = hora % HorasPorDia;
            if (resto < 0)
            {
                resto += HorasPorDia;
            }

            var normalizada = (float)resto;
            return normalizada >= HorasPorDia ? 0f : normalizada;
        }

        /// <summary>
        /// A hora depois de <paramref name="segundos"/> reais, a partir de
        /// <paramref name="hora"/>, num dia que dura <paramref name="duracaoDoDiaSegundos"/>.
        /// Dá a volta à meia-noite. Tempo negativo ou não finito não anda; duração inválida usa
        /// <see cref="DuracaoDoDiaSegundos"/>.
        /// </summary>
        public static float HoraApos(double hora, double segundos, double duracaoDoDiaSegundos)
        {
            if (double.IsNaN(segundos) || double.IsInfinity(segundos) || segundos <= 0)
            {
                return Normalizar(hora);
            }

            var duracao = DuracaoValida(duracaoDoDiaSegundos);
            var voltas = segundos / duracao;
            var fracao = voltas - Math.Floor(voltas);
            return Normalizar(Normalizar(hora) + fracao * HorasPorDia);
        }

        /// <summary>O mesmo de <see cref="HoraApos(double, double, double)"/> com a duração padrão.</summary>
        public static float HoraApos(double hora, double segundos) =>
            HoraApos(hora, segundos, DuracaoDoDiaSegundos);

        /// <summary>
        /// É noite? Vale de <see cref="InicioDaNoite"/> (inclusive) até <see cref="FimDaNoite"/>
        /// (exclusive), passando pela meia-noite. É o intervalo em que as lanternas ficam acesas.
        /// </summary>
        public static bool EhNoite(double hora)
        {
            var h = Normalizar(hora);
            return h >= InicioDaNoite || h < FimDaNoite;
        }

        /// <summary>A parte do dia já passada, de 0 (meia-noite) até menos de 1.</summary>
        public static float FracaoDoDia(double hora) => Normalizar(hora) / HorasPorDia;

        /// <summary>A duração recebida, se serve (finita e maior que zero); senão a padrão.</summary>
        public static double DuracaoValida(double duracaoDoDiaSegundos)
        {
            if (double.IsNaN(duracaoDoDiaSegundos) || double.IsInfinity(duracaoDoDiaSegundos))
            {
                return DuracaoDoDiaSegundos;
            }

            return duracaoDoDiaSegundos > 0 ? duracaoDoDiaSegundos : DuracaoDoDiaSegundos;
        }
    }
}
