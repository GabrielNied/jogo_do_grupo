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

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoDescartar"/> destrói o exemplar da
        /// bolsa e responde 200 <see cref="EquipamentosResponse"/> com o estado atualizado.
        /// </summary>
        public const string Descartar = "/api/personagens/{personagemId}/inventario/descartar";

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoMover"/> move o exemplar para uma casa
        /// da bolsa (casa ocupada: troca) e responde 200 <see cref="EquipamentosResponse"/>.
        /// </summary>
        public const string Mover = "/api/personagens/{personagemId}/inventario/mover";

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoOrdenar"/> reordena uma aba da bolsa
        /// (tipo, tier, nível; favoritos ficam onde estão) e responde 200
        /// <see cref="EquipamentosResponse"/>. Aba fora da capacidade: 400 <c>CasaInvalida</c>.
        /// </summary>
        public const string Ordenar = "/api/personagens/{personagemId}/inventario/ordenar";

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoDescartarLote"/> destrói múltiplos
        /// exemplares da bolsa em lote (ou todos marcados como lixo) e responde 200
        /// <see cref="EquipamentosResponse"/>.
        /// </summary>
        public const string DescartarLote =
            "/api/personagens/{personagemId}/inventario/descartar-lote";

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoLixo"/> marca/desmarca itens como
        /// lixo e responde 200 <see cref="EquipamentosResponse"/>.
        /// </summary>
        public const string Lixo = "/api/personagens/{personagemId}/inventario/lixo";

        /// <summary>
        /// Modelo da rota. <c>POST</c> com <see cref="PedidoFavorito"/> marca/desmarca itens como
        /// favorito (protegidos) e responde 200 <see cref="EquipamentosResponse"/>.
        /// </summary>
        public const string Favorito = "/api/personagens/{personagemId}/inventario/favorito";

        /// <summary>Caminho de <see cref="DescartarLote"/> para um personagem (uso do client).</summary>
        public static string DescartarLoteDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/descartar-lote";

        /// <summary>Caminho de <see cref="Lixo"/> para um personagem (uso do client).</summary>
        public static string LixoDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/lixo";

        /// <summary>Caminho de <see cref="Favorito"/> para um personagem (uso do client).</summary>
        public static string FavoritoDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/favorito";

        /// <summary>Caminho de <see cref="Ordenar"/> para um personagem (uso do client).</summary>
        public static string OrdenarDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/ordenar";

        /// <summary>Caminho de <see cref="Mover"/> para um personagem (uso do client).</summary>
        public static string MoverDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/mover";

        /// <summary>Caminho de <see cref="Descartar"/> para um personagem (uso do client).</summary>
        public static string DescartarDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario/descartar";

        /// <summary>Caminho de <see cref="Inventario"/> para um personagem (uso do client).</summary>
        public static string InventarioDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/inventario";

        /// <summary>Caminho de <see cref="Bau"/> para um personagem (uso do client).</summary>
        public static string BauDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/bau";
    }
}
