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

        /// <summary>
        /// Se o personagem lê o input de movimento: com janelas abertas, sim (anda com elas); só o
        /// menu do Esc, a entrada pendente, a queda ou a digitação de texto param o movimento.
        /// </summary>
        internal static bool DeveLerInput(
            bool menuAberto,
            bool entradaPendente,
            bool conectado,
            bool entradaDeTextoAtiva = false
        ) => !menuAberto && !entradaPendente && conectado && !entradaDeTextoAtiva;

        /// <summary>
        /// Fornece decisão explícita sobre a leitura de input de movimento do avatar.
        /// </summary>
        internal static bool DeveLerInputDeMovimento(
            bool menuAberto,
            bool entradaPendente,
            bool conectado,
            bool entradaDeTextoAtiva
        ) => DeveLerInput(menuAberto, entradaPendente, conectado, entradaDeTextoAtiva);

        /// <summary>
        /// Com janela aberta, se o botão direito começa a girar a câmera: só fora das janelas.
        /// </summary>
        internal static bool ComecaAGirar(bool botaoApertado, bool sobreJanela) =>
            botaoApertado && !sobreJanela;

        /// <summary>
        /// Com janela aberta, se o clique esquerdo começa no mundo (fora da interface): ao
        /// soltar, entra no modo combate (cursor preso). Clique na interface não ataca.
        /// </summary>
        internal static bool ComecaCliqueNoMundo(bool botaoApertado, bool sobreInterface) =>
            botaoApertado && !sobreInterface;

        /// <summary>
        /// Se o Esc só sai do modo combate (solta o cursor) em vez de fechar a janela de cima:
        /// um Esc nunca faz as duas coisas.
        /// </summary>
        internal static bool EscSaiDoModoCombate(bool modoCombate, bool janelaAberta) =>
            modoCombate && janelaAberta;
    }
}
