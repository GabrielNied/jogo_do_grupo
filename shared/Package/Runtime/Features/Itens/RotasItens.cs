namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Rotas HTTP de Itens. Todas exigem o header <c>Authorization: Bearer &lt;token&gt;</c> e
    /// um personagem da conta do token. As rotas são HTTP e não usam IDs de mensagem
    /// (<see cref="ItensMessageIds.Faixa"/>). O <c>{personagemId}</c> é o
    /// <c>PersonagemResumo.Id</c> (long).
    /// </summary>
    public static class RotasItens
    {
        /// <summary>
        /// Modelo da rota (para registrar no servidor). <c>GET</c> responde 200
        /// <see cref="InventarioResponse"/>.
        /// </summary>
        public const string Inventario = "/api/personagens/{personagemId}/inventario";

        /// <summary>
        /// Modelo da rota. <c>POST</c> sem corpo responde 200 <see cref="AbrirBauResponse"/>.
        /// </summary>
        public const string Bau = "/api/personagens/{personagemId}/bau";

        /// <summary>Caminho de <see cref="Inventario"/> para um personagem (uso do client).</summary>
        public static string InventarioDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario";

        /// <summary>Caminho de <see cref="Bau"/> para um personagem (uso do client).</summary>
        public static string BauDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/bau";
    }
}
