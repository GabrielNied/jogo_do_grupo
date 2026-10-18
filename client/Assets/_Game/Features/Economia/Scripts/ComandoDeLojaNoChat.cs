using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Features.Economia.UI;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>
    /// Comando <c>/loja</c> do chat para abrir a janela da loja e de reparo do NPC.
    /// </summary>
    internal static class ComandoDeLojaNoChat
    {
        public const string Verbo = "loja";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                Verbo,
                Executar,
                "/loja — abre a loja do NPC e serviços de reparo"
            );
        }

        private static ResultadoDeComandoDeBarra Executar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (JanelasDoJogo.EstaAberta(JanelaLoja.Chave))
            {
                JanelasDoJogo.Fechar(JanelaLoja.Chave);
            }
            else
            {
                JanelasDoJogo.Abrir(JanelaLoja.Chave);
            }

            return new ResultadoDeComandoDeBarra();
        }
    }
}
