using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto
{
    /// <summary>
    /// Valores das marcas de impacto no chão (regra 17), só visuais: ficam na <see cref="Sintonia"/>
    /// (painel F10) e o valor aqui é o padrão do jogo. Não valem para jogabilidade.
    /// </summary>
    internal static class ConfigMarcasDeImpacto
    {
        private const string Grupo = "Mundo";

        /// <summary>Quanto cada marca fica no chão, em segundos, contando o fade.</summary>
        public static float DuracaoSegundos = 20f;

        /// <summary>Os últimos segundos da marca, em que ela some aos poucos.</summary>
        public static float FadeSegundos = 4f;

        /// <summary>Marcas ao mesmo tempo; passando disso a mais antiga sai primeiro.</summary>
        public static float MaximoDeMarcas = 32f;

        /// <summary>Diâmetro da marca de martelada, em metros.</summary>
        public static float DiametroMartelada = 2.2f;

        /// <summary>Diâmetro da marca de fuligem, em metros.</summary>
        public static float DiametroFuligem = 1.6f;

        /// <summary>Opacidade máxima da marca, de 0 a 1.</summary>
        public static float OpacidadeMaxima = 0.8f;

        /// <summary>Folga acima do chão para a marca não brigar com o relevo, em metros.</summary>
        public const float FolgaDoChao = 0.03f;

        /// <summary>Quanto tempo um projétil de fogo espera o acerto antes de ser esquecido, em segundos.</summary>
        public const float ValidadeDoProjetilSegundos = 6f;

        public static int Teto => Mathf.Max(1, Mathf.RoundToInt(MaximoDeMarcas));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            Sintonia.Registrar(
                Grupo,
                "MarcaDuracao",
                () => DuracaoSegundos,
                v => DuracaoSegundos = v,
                1f,
                60f,
                "s que a marca de impacto fica no chão"
            );
            Sintonia.Registrar(
                Grupo,
                "MarcaFade",
                () => FadeSegundos,
                v => FadeSegundos = v,
                0f,
                20f,
                "s finais em que a marca some aos poucos"
            );
            Sintonia.Registrar(
                Grupo,
                "MarcaMaximo",
                () => MaximoDeMarcas,
                v => MaximoDeMarcas = v,
                1f,
                64f,
                "marcas de impacto ao mesmo tempo"
            );
            Sintonia.Registrar(
                Grupo,
                "MarcaDiametroMartelada",
                () => DiametroMartelada,
                v => DiametroMartelada = v,
                0.5f,
                6f,
                "m de diâmetro da marca de martelada"
            );
            Sintonia.Registrar(
                Grupo,
                "MarcaDiametroFuligem",
                () => DiametroFuligem,
                v => DiametroFuligem = v,
                0.5f,
                6f,
                "m de diâmetro da marca de fuligem"
            );
            Sintonia.Registrar(
                Grupo,
                "MarcaOpacidade",
                () => OpacidadeMaxima,
                v => OpacidadeMaxima = v,
                0.1f,
                1f,
                "opacidade máxima das marcas"
            );
        }
    }
}
