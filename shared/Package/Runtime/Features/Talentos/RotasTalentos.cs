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

        /// <summary>
        /// Modelo da rota. <c>POST</c>: corpo <see cref="RedistribuirRequest"/>, redistribui todos os pontos
        /// da árvore mediante custo em coroas (200 <see cref="ArvoreResponse"/>).
        /// </summary>
        public const string Redistribuir = "/api/personagens/{personagemId}/talentos/redistribuir";

        /// <summary>Caminho de <see cref="Redistribuir"/> para um personagem (uso do client).</summary>
        public static string RedistribuirDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/talentos/redistribuir";

        /// <summary>
        /// Modelo da rota de perfis. <c>GET</c> lista perfis (200 <see cref="ListaPerfisResponse"/>).
        /// <c>POST</c> salva o estado atual como perfil (200 <see cref="PerfilTalentosDto"/>).
        /// </summary>
        public const string Perfis = "/api/personagens/{personagemId}/talentos/perfis";

        /// <summary>Caminho de <see cref="Perfis"/> para um personagem (uso do client).</summary>
        public static string PerfisDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/talentos/perfis";

        /// <summary>
        /// Modelo da rota de perfil individual. <c>PUT</c> renomeia (<see cref="RenomearPerfilRequest"/>).
        /// <c>DELETE</c> remove o perfil.
        /// </summary>
        public const string Perfil = "/api/personagens/{personagemId}/talentos/perfis/{perfilId}";

        /// <summary>Caminho de <see cref="Perfil"/> para um personagem e perfil (uso do client).</summary>
        public static string PerfilDe(long personagemId, long perfilId) =>
            "/api/personagens/" + personagemId + "/talentos/perfis/" + perfilId;

        /// <summary>
        /// Modelo da rota para carregar perfil. <c>POST</c> aplica o perfil salvo na árvore
        /// (200 <see cref="CarregarPerfilResponse"/>).
        /// </summary>
        public const string CarregarPerfil =
            "/api/personagens/{personagemId}/talentos/perfis/{perfilId}/carregar";

        /// <summary>Caminho de <see cref="CarregarPerfil"/> para um personagem e perfil (uso do client).</summary>
        public static string CarregarPerfilDe(long personagemId, long perfilId) =>
            "/api/personagens/" + personagemId + "/talentos/perfis/" + perfilId + "/carregar";
    }
}
