namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Traçado autoral salvo pelo editor para a ligação da cidade central ao Passo.</summary>
    public static class EstradaDoPasso
    {
        public static readonly TracadoDoMundo Tracado = new TracadoDoMundo(
            new[]
            {
                new TracadoDoMundo.Ponto(0f, 42f),
                new TracadoDoMundo.Ponto(-75f, 105f),
                new TracadoDoMundo.Ponto(-205f, 145f),
                new TracadoDoMundo.Ponto(-345f, 205f),
                new TracadoDoMundo.Ponto(-520f, 320f),
            },
            7f,
            8f,
            18f
        );
    }
}
