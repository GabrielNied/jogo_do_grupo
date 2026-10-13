namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Códigos de erro de Talentos em <c>ErroApi.Codigo</c>, com o status HTTP ao lado.</summary>
    public static class CodigosErroTalentos
    {
        /// <summary>400: o nó não existe na configuração.</summary>
        public const string NoInvalido = "NoInvalido";

        /// <summary>400: o rank alvo é menor que 1 ou pula ranks (mais de um acima do atual).</summary>
        public const string RankAlvoInvalido = "RankAlvoInvalido";

        /// <summary>409: o nó já está no rank máximo.</summary>
        public const string RankMaximo = "RankMaximo";

        /// <summary>409: falta cumprir requisito (nó anterior).</summary>
        public const string RequisitoNaoCumprido = "RequisitoNaoCumprido";

        /// <summary>409: pontos da árvore livres menores que o custo do rank.</summary>
        public const string PontosInsuficientes = "PontosInsuficientes";

        /// <summary>
        /// 409: a versão da configuração do pedido ou da árvore salva do personagem não é a
        /// atual. Nada é cobrado; o client reabre a árvore.
        /// </summary>
        public const string ArvoreDesatualizada = "ArvoreDesatualizada";
    }
}
