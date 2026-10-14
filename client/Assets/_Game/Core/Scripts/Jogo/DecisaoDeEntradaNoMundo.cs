namespace JogoDoGrupo.Client.Core.Jogo
{
    public enum DecisaoDoEsc
    {
        IgnorarPorEntradaDeTexto,
        IgnorarPorConsumoAnterior,
        VoltarDoSubmenu,
        AlternarMenu,
    }

    internal static class DecisaoDeEntradaNoMundo
    {
        internal static DecisaoDoEsc DecidirEsc(
            bool campoComFoco,
            bool bloqueadoPorTexto,
            bool escConsumido,
            bool submenuAberto
        )
        {
            if (campoComFoco || bloqueadoPorTexto)
            {
                return DecisaoDoEsc.IgnorarPorEntradaDeTexto;
            }

            if (escConsumido)
            {
                return DecisaoDoEsc.IgnorarPorConsumoAnterior;
            }

            return submenuAberto ? DecisaoDoEsc.VoltarDoSubmenu : DecisaoDoEsc.AlternarMenu;
        }

        internal static bool PodeExecutarAtalho(bool campoComFoco, bool bloqueadoPorTexto) =>
            !campoComFoco && !bloqueadoPorTexto;

        internal static bool DeveCapturarCursor(
            bool algoAberto,
            bool entradaPendente,
            bool conectado
        ) => !algoAberto && !entradaPendente && conectado;
    }
}
