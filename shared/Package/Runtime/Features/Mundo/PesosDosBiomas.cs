namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Quanto de cada bioma existe num ponto do mundo. Os sete pesos somam 1. Longe das
    /// fronteiras um deles vale 1; perto delas dois ou mais se misturam, e é essa mistura que
    /// deixa o relevo e as cores sem degrau.
    /// </summary>
    public readonly struct PesosDosBiomas
    {
        public PesosDosBiomas(
            float campo,
            float mata,
            float colinas,
            float deserto,
            float neve,
            float lava,
            float pantano
        )
        {
            Campo = campo;
            Mata = mata;
            Colinas = colinas;
            Deserto = deserto;
            Neve = neve;
            Lava = lava;
            Pantano = pantano;
        }

        public float Campo { get; }

        public float Mata { get; }

        public float Colinas { get; }

        public float Deserto { get; }

        public float Neve { get; }

        public float Lava { get; }

        public float Pantano { get; }

        /// <summary>O bioma de maior peso. Nunca devolve <see cref="Bioma.Cidade"/>.</summary>
        public Bioma Dominante
        {
            get
            {
                var bioma = Bioma.Campo;
                var maior = Campo;
                Comparar(Bioma.Mata, Mata, ref bioma, ref maior);
                Comparar(Bioma.Colinas, Colinas, ref bioma, ref maior);
                Comparar(Bioma.Deserto, Deserto, ref bioma, ref maior);
                Comparar(Bioma.Neve, Neve, ref bioma, ref maior);
                Comparar(Bioma.Lava, Lava, ref bioma, ref maior);
                Comparar(Bioma.Pantano, Pantano, ref bioma, ref maior);
                return bioma;
            }
        }

        /// <summary>Peso de <paramref name="bioma"/>. A cidade não tem peso próprio (0).</summary>
        public float De(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Campo:
                    return Campo;
                case Bioma.Mata:
                    return Mata;
                case Bioma.Colinas:
                    return Colinas;
                case Bioma.Deserto:
                    return Deserto;
                case Bioma.Neve:
                    return Neve;
                case Bioma.Lava:
                    return Lava;
                case Bioma.Pantano:
                    return Pantano;
                default:
                    return 0f;
            }
        }

        private static void Comparar(Bioma candidato, float peso, ref Bioma bioma, ref float maior)
        {
            if (peso > maior)
            {
                bioma = candidato;
                maior = peso;
            }
        }
    }
}
