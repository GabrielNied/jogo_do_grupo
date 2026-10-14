namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>Tom da etiqueta de nível conforme a diferença inimigo − jogador.</summary>
    public enum TomDePerigoDoNivel
    {
        Trivial,
        Normal,
        Atencao,
        Mortal,
    }

    /// <summary>Faixas de cor da etiqueta (proposta da issue #484, calibrável).</summary>
    public static class RegrasCorDePerigo
    {
        /// <summary>Inimigo até esta diferença abaixo do jogador fica trivial (cinza).</summary>
        public const int DiferencaTrivialMax = -3;

        /// <summary>Inimigo a partir desta diferença acima do jogador fica atenção (amarelo).</summary>
        public const int DiferencaAtencaoMin = 1;

        /// <summary>Inimigo a partir desta diferença acima do jogador fica mortal (vermelho).</summary>
        public const int DiferencaMortalMin = 4;

        public static TomDePerigoDoNivel Tom(int nivelJogador, int nivelInimigo)
        {
            var diferenca = nivelInimigo - nivelJogador;
            if (diferenca <= DiferencaTrivialMax)
            {
                return TomDePerigoDoNivel.Trivial;
            }

            if (diferenca >= DiferencaMortalMin)
            {
                return TomDePerigoDoNivel.Mortal;
            }

            if (diferenca >= DiferencaAtencaoMin)
            {
                return TomDePerigoDoNivel.Atencao;
            }

            return TomDePerigoDoNivel.Normal;
        }
    }
}
