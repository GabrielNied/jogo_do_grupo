namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Rotas HTTP da árvore de atributos. Todas exigem o header
    /// <c>Authorization: Bearer &lt;token&gt;</c> e um personagem da conta do token.
    /// </summary>
    public static class RotasTalentos
    {
        /// <summary>
        /// Modelo da rota. <c>GET</c> responde 200 <see cref="ArvoreResponse"/>. Erros: 404
        /// <c>PersonagemNaoEncontrado</c> (código de Personagem) e 401 do Core.
        /// </summary>
        public const string Arvore = "/api/personagens/{personagemId}/talentos";

        /// <summary>Caminho de <see cref="Arvore"/> para um personagem (uso do client).</summary>
        public static string ArvoreDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/talentos";

        /// <summary>
        /// Modelo da rota. <c>POST</c>: corpo <see cref="ComprarNoRequest"/>, compra um rank
        /// (200 <see cref="ArvoreResponse"/>).
        /// </summary>
        public const string Comprar = "/api/personagens/{personagemId}/talentos/comprar";

        /// <summary>Caminho de <see cref="Comprar"/> para um personagem (uso do client).</summary>
        public static string ComprarDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/talentos/comprar";
    }
}
