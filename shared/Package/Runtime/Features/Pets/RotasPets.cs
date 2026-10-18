namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Rotas HTTP do módulo de pets (#1068).
    /// </summary>
    public static class RotasPets
    {
        public const string OfertasCuidador = "/api/pets/cuidador";
        public const string ComprarCuidador =
            "/api/personagens/{personagemId}/pets/cuidador/comprar";
    }
}
