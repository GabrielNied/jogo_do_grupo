namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Rotas HTTP da funcionalidade de montarias (#1067).
    /// </summary>
    public static class RotasMontarias
    {
        public const string Estabulo = "/api/montarias/estabulo";
        public const string ComprarEstabulo =
            "/api/personagens/{personagemId}/montarias/estabulo/comprar";

        public static string ComprarEstabuloDe(long personagemId) =>
            $"/api/personagens/{personagemId}/montarias/estabulo/comprar";
    }
}
