namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Rotas HTTP de Combate. Exigem <c>Authorization: Bearer &lt;token&gt;</c> e um personagem
    /// da conta do token. Não usam IDs de mensagem (<see cref="CombateMessageIds.Faixa"/>).
    /// O <c>{personagemId}</c> é o <c>PersonagemResumo.Id</c> (long).
    /// </summary>
    public static class RotasCombate
    {
        /// <summary>
        /// Modelo da rota. <c>GET</c> responde 200 <see cref="RecursosResponse"/>. Erros:
        /// <see cref="CodigosErroCombate.PersonagemNaoEncontrado"/> (404) e 401 do Core.
        /// </summary>
        public const string Recursos = "/api/personagens/{personagemId}/recursos";

        /// <summary>Caminho de <see cref="Recursos"/> para um personagem (uso do client).</summary>
        public static string RecursosDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/recursos";
    }
}
