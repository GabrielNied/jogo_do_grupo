namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Faixa válida de um atributo de item numa categoria de peça, na escala interna de 0 a 100
    /// (TIER-006). <see cref="Minimo"/> e <see cref="Maximo"/> são absolutos: o grau não os muda.
    /// </summary>
    public readonly struct FaixaDeAtributo
    {
        public readonly int Minimo;
        public readonly int Maximo;

        public FaixaDeAtributo(int minimo, int maximo)
        {
            Minimo = minimo;
            Maximo = maximo;
        }

        public int Amplitude => Maximo - Minimo;

        /// <summary>O valor limitado à faixa.</summary>
        public int Limitar(double valor)
        {
            var inteiro = (int)System.Math.Round(valor, System.MidpointRounding.AwayFromZero);
            return inteiro < Minimo ? Minimo
                : inteiro > Maximo ? Maximo
                : inteiro;
        }
    }
}
