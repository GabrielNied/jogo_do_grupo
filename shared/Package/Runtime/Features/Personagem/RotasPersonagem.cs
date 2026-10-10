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
    }
}
