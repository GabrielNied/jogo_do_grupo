using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// A primeira caverna: uma boca nas colinas e um interior à parte (não é um buraco no
    /// chão do mundo, nem a dungeon da issue #66). Entrar e sair devolve ao mesmo ponto.
    /// </summary>
    public static class PrimeiraCaverna
    {
        /// <summary>Metade da largura do corredor, em metros.</summary>
        public const float MeiaLargura = 2.5f;

        /// <summary>Raio da boca, no mundo e na saída do interior, em metros.</summary>
        public const float RaioDaBoca = 2f;

        /// <summary>Boca no mundo, em X.</summary>
        public const float EntradaX = -220f;

        /// <summary>Boca no mundo, em Z.</summary>
        public const float EntradaZ = -80f;

        /// <summary>Piso do interior, abaixo do relevo, para não encostar no chão de fora.</summary>
        public const float InteriorY = -40f;

        /// <summary>Origem do interior, em X (a mesma coluna da boca, noutro piso).</summary>
        public const float InteriorX = -220f;

        /// <summary>Boca do interior, em Z. A saída fica aqui.</summary>
        public const float InteriorZ = -80f;

        /// <summary>Onde a pessoa aparece ao entrar, em X: o centro do corredor.</summary>
        public const float NascimentoX = InteriorX;

        /// <summary>Onde a pessoa aparece ao entrar: 4 m corredor adentro, no centro.</summary>
        public const float NascimentoZ = -76f;

        /// <summary>Altura do chão do mundo na boca.</summary>
        public static float EntradaY => MapaDoMundo.AlturaEm(EntradaX, EntradaZ);

        /// <summary>Distância horizontal entre dois pontos.</summary>
        public static float Distancia(float x, float z, float outroX, float outroZ)
        {
            var dx = x - outroX;
            var dz = z - outroZ;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Se o ponto está na boca do lado de fora.</summary>
        public static bool NaBoca(float x, float z) =>
            Distancia(x, z, EntradaX, EntradaZ) <= RaioDaBoca;

        /// <summary>Se o ponto está na saída, dentro do corredor.</summary>
        public static bool NaSaida(float x, float z) =>
            Distancia(x, z, InteriorX, InteriorZ) <= RaioDaBoca;

        /// <summary>
        /// Folga do nascimento até a parede do corredor, em metros. Maior que o raio do
        /// personagem: ele não nasce dentro da parede.
        /// </summary>
        public static float FolgaDoNascimento()
        {
            var ateEsquerda = Math.Abs(InteriorX - MeiaLargura - NascimentoX);
            var ateDireita = Math.Abs(InteriorX + MeiaLargura - NascimentoX);
            return (float)Math.Min(ateEsquerda, ateDireita);
        }
    }
}
