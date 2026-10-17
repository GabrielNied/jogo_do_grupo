namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Regiões salvas pelo criador de mapas. A ordem define a sobreposição.</summary>
    public static class RegioesDoMundo
    {
        public static readonly RegiaoDeBioma[] Todas =
        {
            new RegiaoDeBioma(
                "Mata de exemplo",
                Bioma.Mata,
                new[]
                {
                    new RegiaoDeBioma.Ponto(170f, -130f),
                    new RegiaoDeBioma.Ponto(310f, -100f),
                    new RegiaoDeBioma.Ponto(330f, -250f),
                    new RegiaoDeBioma.Ponto(160f, -270f),
                },
                70f,
                1.1f,
                1f
            ),
            new RegiaoDeBioma(
                "Floresta01",
                Bioma.Mata,
                new[]
                {
                    new RegiaoDeBioma.Ponto(-2.616f, 59.119f),
                    new RegiaoDeBioma.Ponto(-72.22f, 113.639f),
                    new RegiaoDeBioma.Ponto(-293.187f, 195.69f),
                    new RegiaoDeBioma.Ponto(-278.486f, 548.352f),
                    new RegiaoDeBioma.Ponto(-33.663f, 528.607f),
                    new RegiaoDeBioma.Ponto(291.769f, 386.075f),
                    new RegiaoDeBioma.Ponto(288.593f, 168.715f),
                    new RegiaoDeBioma.Ponto(91.868f, 178.591f),
                },
                30f,
                1.47f,
                1f
            ),
        };
    }
}
