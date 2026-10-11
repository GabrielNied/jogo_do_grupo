namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Velocidades de movimento (m/s). O servidor é o dono da velocidade de cada personagem
    /// (<see cref="VelocidadesDeMovimento"/>); estes são os valores de quem acabou de entrar.
    /// </summary>
    public static class RegrasVelocidade
    {
        public const float AndarPadrao = 2f;
        public const float CorrerPadrao = 5.5f;

        /// <summary>Faixa aceita no pedido de teste (<see cref="PedirVelocidadeDeTeste"/>).</summary>
        public const float TesteMinima = 0.5f;
        public const float TesteMaxima = 50f;

        /// <summary>Se o pedido de teste está dentro da faixa (valores finitos).</summary>
        public static bool TesteValido(float andar, float correr)
        {
            return Dentro(andar) && Dentro(correr);
        }

        /// <summary>A maior das duas: é o limite que o servidor usa para validar o deslocamento.</summary>
        public static float Maxima(float andar, float correr)
        {
            return andar > correr ? andar : correr;
        }

        private static bool Dentro(float valor)
        {
            return !float.IsNaN(valor) && valor >= TesteMinima && valor <= TesteMaxima;
        }
    }
}
