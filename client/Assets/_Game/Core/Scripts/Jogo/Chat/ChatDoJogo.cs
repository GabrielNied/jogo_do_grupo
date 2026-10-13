using System;

namespace JogoDoGrupo.Client.Core.Jogo.Chat
{
    /// <summary>
    /// Ponte entre funcionalidades e a caixa de texto do chat, sem referência entre features.
    /// O Social registra os delegates quando monta o painel.
    /// </summary>
    public static class ChatDoJogo
    {
        /// <summary>Abre a caixa com texto opcional (ex.: F12 com <c>/</c>).</summary>
        public static Action<string> AbrirCaixaDeTexto { get; set; }

        /// <summary>F12: abre com texto inicial ou fecha se já estiver aberta.</summary>
        public static Action AlternarCaixaDeTexto { get; set; }

        /// <summary>Linha de Sistema no histórico do chat (ex.: resposta de comando de teste).</summary>
        public static Action<string> AdicionarLinhaSistema { get; set; }

        public static void PedirAbrir(string textoInicial = "")
        {
            AbrirCaixaDeTexto?.Invoke(textoInicial ?? "");
        }

        public static void PedirAlternar()
        {
            AlternarCaixaDeTexto?.Invoke();
        }
    }
}
