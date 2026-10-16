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
                2f
            ),
        };
    }
}
