#nullable enable

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Regras puras da distribuição de pontos de atributo (fatia jogável, seção 6). O client usa
    /// para avisar antes de enviar; o servidor valida de novo, com os pontos livres que ele
    /// guarda, e é quem gasta.
    /// </summary>
    public static class RegrasAtributos
    {
        /// <summary>Soma dos pontos pedidos. Usa <c>long</c> para a soma não estourar.</summary>
        public static long Total(PontosDeAtributos pontos) =>
            (long)pontos.Forca
            + pontos.Destreza
            + pontos.Intelecto
            + pontos.Vitalidade
            + pontos.Espirito;

        /// <summary>
        /// Confere um pedido de gasto contra os pontos livres do personagem.
        /// </summary>
        /// <returns>
        /// <see cref="CodigosErroPersonagem.AtributoInvalido"/> se o pedido é nulo, tem valor
        /// negativo ou não gasta nada; <see cref="CodigosErroPersonagem.PontosInsuficientes"/> se
        /// pede mais do que há; <c>null</c> se pode gastar.
        /// </returns>
        public static string? ValidarDistribuicao(int pontosLivres, PontosDeAtributos? pontos)
        {
            if (
                pontos == null
                || pontos.Forca < 0
                || pontos.Destreza < 0
                || pontos.Intelecto < 0
                || pontos.Vitalidade < 0
                || pontos.Espirito < 0
            )
            {
                return CodigosErroPersonagem.AtributoInvalido;
            }

            var total = Total(pontos);
            if (total == 0)
            {
                return CodigosErroPersonagem.AtributoInvalido;
            }

            return total > pontosLivres ? CodigosErroPersonagem.PontosInsuficientes : null;
        }
    }
}
