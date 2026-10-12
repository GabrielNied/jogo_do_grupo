using System;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Regras puras das manadas: o limite urbano (a manada nunca entra na cidade) e o limite
    /// de perseguição. Valores da proposta, a calibrar (docs/features/mobs.md, "Manadas com
    /// rota"). O servidor decide; o client pode usar para mostrar o limite.
    /// </summary>
    public static class RegrasManada
    {
        /// <summary>
        /// Faixa, em metros, além dos muros em que a manada já não entra nem enxerga ninguém:
        /// a borda urbana fica a <see cref="MapaDoMundo.MetadeDaCidade"/> mais esta margem do
        /// centro. Mantém o golpe, que alcança alguns metros, longe dos moradores.
        /// </summary>
        public const float MargemUrbanaMetros = 10f;

        /// <summary>Quanto a manada pode se afastar do seu ponto antes de desistir, em metros.</summary>
        public const double LimiteDePerseguicaoMetros = 80.0;

        /// <summary>Velocidade da manada seguindo a rota, em metros por segundo (andando).</summary>
        public const double VelocidadeDeRotaMetrosPorSegundo = 1.5;

        /// <summary>Metade do lado da zona urbana (cidade mais a margem), em metros.</summary>
        public static float MetadeDaZonaUrbana => MapaDoMundo.MetadeDaCidade + MargemUrbanaMetros;

        /// <summary>
        /// Se o ponto está na zona urbana: o quadrado da cidade mais a margem. Lá a manada não
        /// entra, não vê ninguém e não ataca (moradores e comerciantes ficam a salvo).
        /// </summary>
        public static bool EstaNaZonaUrbana(double x, double z)
        {
            double metade = MetadeDaZonaUrbana;
            return Math.Abs(x) <= metade && Math.Abs(z) <= metade;
        }

        /// <summary>Se a perseguição passou do limite e a manada deve voltar ao seu ponto.</summary>
        public static bool DeveDesistir(double distanciaDaOrigemMetros, double limiteMetros) =>
            distanciaDaOrigemMetros > limiteMetros;
    }
}
