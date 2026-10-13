namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Raio de interesse do broadcast de inimigos (issue #485). Histerese: entra a 80 m, sai a
    /// 100 m. Etiquetas 3D no client só até <see cref="EtiquetaMetros"/>.
    /// </summary>
    public static class RegrasInteresseDeInimigos
    {
        public const double EntradaMetros = 80.0;
        public const double SaidaMetros = 100.0;
        public const double EtiquetaMetros = 40.0;

        /// <summary>Se a mensagem de rede deve ir ao jogador, dado o que ele já recebia.</summary>
        public static bool DeveEnviar(double distanciaMetros, bool jaRecebia)
        {
            return jaRecebia ? distanciaMetros <= SaidaMetros : distanciaMetros <= EntradaMetros;
        }

        public static double DistanciaAoJogador(
            double jogadorX,
            double jogadorZ,
            double inimigoX,
            double inimigoZ
        ) => RegrasInimigo.Distancia(jogadorX, jogadorZ, inimigoX, inimigoZ);
    }
}
