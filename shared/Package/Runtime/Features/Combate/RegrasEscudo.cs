using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Contas puras do escudo (#170): quanto ele absorve e como o dano o consome. O escudo
    /// absorve antes da vida; o que ele não cobre passa para a vida.
    /// </summary>
    public static class RegrasEscudo
    {
        /// <summary>Absorção de um escudo novo para alguém com <paramref name="vidaMaxima"/>.</summary>
        public static int Absorcao(int vidaMaxima, double fracao)
        {
            if (vidaMaxima <= 0 || fracao <= 0)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(vidaMaxima * fracao));
        }

        /// <summary>
        /// Aplica <paramref name="dano"/> sobre o escudo com <paramref name="escudo"/> de absorção:
        /// devolve o dano que sobra para a vida e quanto resta de escudo.
        /// </summary>
        public static int Absorver(int escudo, int dano, out int escudoRestante)
        {
            if (dano <= 0 || escudo <= 0)
            {
                escudoRestante = Math.Max(0, escudo);
                return Math.Max(0, dano);
            }

            var absorvido = Math.Min(escudo, dano);
            escudoRestante = escudo - absorvido;
            return dano - absorvido;
        }
    }
}
