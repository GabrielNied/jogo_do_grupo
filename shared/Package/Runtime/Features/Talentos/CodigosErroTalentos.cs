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

        /// <summary>409: não é permitido redistribuir pontos ou trocar perfil durante combate.</summary>
        public const string EmCombate = "EmCombate";

        /// <summary>409: saldo insuficiente de coroas para pagar a redistribuição.</summary>
        public const string SaldoInsuficiente = "SaldoInsuficiente";

        /// <summary>400: nenhum ponto foi gasto na árvore para poder redistribuir.</summary>
        public const string NenhumPontoGasto = "NenhumPontoGasto";

        /// <summary>409: atingiu o limite máximo de perfis de build salvos.</summary>
        public const string LimiteDePerfisAtingido = "LimiteDePerfisAtingido";

        /// <summary>400: nome de perfil vazio ou com tamanho acima do limite.</summary>
        public const string NomeDePerfilInvalido = "NomeDePerfilInvalido";

        /// <summary>404: perfil de build não encontrado ou pertence a outro personagem.</summary>
        public const string PerfilNaoEncontrado = "PerfilNaoEncontrado";

        /// <summary>409: perfil incompatível com os pontos ou requisitos atuais da árvore.</summary>
        public const string PerfilIncompativel = "PerfilIncompativel";
    }
}
