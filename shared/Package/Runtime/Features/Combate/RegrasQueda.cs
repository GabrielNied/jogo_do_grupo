using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Regras puras do estado caído: tempos, alcance e vida ao levantar. Valores da proposta em
    /// docs/features/fatia-jogavel.md (seção 7) e docs/features/personagem.md ("Morte e caído").
    /// O servidor decide; o client usa para mostrar a contagem e o botão de voltar.
    /// </summary>
    public static class RegrasQueda
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Segundos que o personagem fica caído antes de voltar sozinho à cidade.</summary>
        public const double DuracaoCaidoSegundos = 45.0;

        /// <summary>Segundos que um aliado precisa segurar a interação para levantar o caído.</summary>
        public const double TempoParaLevantarSegundos = 3.0;

        /// <summary>Fração da vida máxima com que o personagem levanta.</summary>
        public const double FracaoDeVidaAoLevantar = 0.20;

        /// <summary>Distância máxima, em metros, entre o ajudante e o caído (2,0 ainda vale).</summary>
        public const double AlcanceParaLevantarMetros = 2.0;

        /// <summary>Segundos caído antes de poder pedir para voltar à cidade (5,0 já vale).</summary>
        public const double MinimoParaVoltarSegundos = 5.0;

        // ── Regras ────────────────────────────────────────────────────────────────

        public static int DuracaoCaidoMs => Milissegundos(DuracaoCaidoSegundos);

        public static int TempoParaLevantarMs => Milissegundos(TempoParaLevantarSegundos);

        public static int MinimoParaVoltarMs => Milissegundos(MinimoParaVoltarSegundos);

        /// <summary>Vida com que o personagem levanta: 20% do máximo, arredondado para cima, no mínimo 1.</summary>
        public static int VidaAoLevantar(int vidaMaxima)
        {
            if (vidaMaxima <= 0)
            {
                return 1;
            }
            int vida = (int)Math.Ceiling(vidaMaxima * FracaoDeVidaAoLevantar);
            return Math.Max(1, Math.Min(vida, vidaMaxima));
        }

        /// <summary>Se a distância permite levantar (2,0 m vale; mais que isso não).</summary>
        public static bool NoAlcanceParaLevantar(double distanciaMetros) =>
            distanciaMetros >= 0.0 && distanciaMetros <= AlcanceParaLevantarMetros;

        /// <summary>Se já dá para pedir "voltar à cidade" depois de tantos ms caído.</summary>
        public static bool PodeVoltarParaACidade(long msCaido) => msCaido >= MinimoParaVoltarMs;

        /// <summary>Segundos inteiros que faltam (arredondado para cima, nunca negativo).</summary>
        public static int SegundosRestantes(long msRestantes) =>
            msRestantes <= 0 ? 0 : (int)((msRestantes + 999) / 1000);

        private static int Milissegundos(double segundos) => (int)Math.Round(segundos * 1000.0);
    }
}
