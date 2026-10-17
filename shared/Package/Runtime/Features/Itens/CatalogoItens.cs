namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Catálogo fixo de itens de teste (recorte de 2026-10-11). Os ids são estáveis: nunca mude
    /// nem reaproveite um id já publicado. O nome exibido vem daqui, tanto no servidor quanto
    /// no client.
    /// </summary>
    public static class CatalogoItens
    {
        public const int EspadaDeTeste = 1;
        public const int EscudoDeTeste = 2;
        public const int PocaoDeTeste = 3;
        public const int MoedaDeTeste = 4;
        public const int RedeasDoCorcelNoturno = 10;

        /// <summary>Todos os ids do catálogo, em ordem crescente.</summary>
        public static readonly int[] Todos =
        {
            EspadaDeTeste,
            EscudoDeTeste,
            PocaoDeTeste,
            MoedaDeTeste,
            RedeasDoCorcelNoturno,
        };

        /// <summary>
        /// Nome exibido do item, ou <c>null</c> se <paramref name="itemId"/> não está no catálogo.
        /// </summary>
        public static string ObterNome(int itemId)
        {
            switch (itemId)
            {
                case EspadaDeTeste:
                    return "Espada de teste";
                case EscudoDeTeste:
                    return "Escudo de teste";
                case PocaoDeTeste:
                    return "Poção de teste";
                case MoedaDeTeste:
                    return "Moeda de teste";
                case RedeasDoCorcelNoturno:
                    return "Rédeas do Corcel Noturno";
                default:
                    return null;
            }
        }
    }
}
