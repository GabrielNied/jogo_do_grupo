namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Rotas HTTP de personagens. Todas exigem o header <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    public static class RotasPersonagem
    {
        /// <summary>
        /// <c>GET</c>: lista os personagens da conta logada (200 <see cref="ListaPersonagensResponse"/>).
        /// <c>POST</c>: corpo <see cref="CriarPersonagemRequest"/>, cria um personagem
        /// (201 <see cref="PersonagemResumo"/>).
        /// </summary>
        public const string Personagens = "/api/personagens";

        /// <summary>
        /// Modelo da rota. <c>GET</c> responde 200 <see cref="ProgressoResponse"/> de um personagem
        /// da conta do token. Erros: <see cref="CodigosErroPersonagem.PersonagemNaoEncontrado"/>
        /// (404) e 401 do Core. O <c>{personagemId}</c> é o <see cref="PersonagemResumo.Id"/>.
        /// </summary>
        public const string Progresso = "/api/personagens/{personagemId}/progresso";

        /// <summary>Caminho de <see cref="Progresso"/> para um personagem (uso do client).</summary>
        public static string ProgressoDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/progresso";

        /// <summary>
        /// Modelo da rota. <c>POST</c>: corpo <see cref="DistribuirAtributosRequest"/>, gasta pontos
        /// livres nos atributos (200 <see cref="ProgressoResponse"/>).
        /// </summary>
        public const string Atributos = "/api/personagens/{personagemId}/atributos";

        /// <summary>Caminho de <see cref="Atributos"/> para um personagem (uso do client).</summary>
        public static string AtributosDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/atributos";
    }
}
